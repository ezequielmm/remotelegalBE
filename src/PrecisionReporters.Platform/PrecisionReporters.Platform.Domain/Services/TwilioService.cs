using FluentResults;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Twilio;
using Twilio.Base;
using Twilio.Jwt.AccessToken;
using Twilio.Rest.Video.V1;
using Twilio.Rest.Video.V1.Room;
using static Twilio.Rest.Video.V1.CompositionResource;
using static Twilio.Rest.Video.V1.RoomResource;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TwilioService : ITwilioService
    {
        private readonly TwilioAccountConfiguration _twilioAccountConfiguration;
        private readonly ILogger<TwilioService> _log;
        private readonly IAwsStorageService _awsStorageService;
        private JsonSerializerSettings _serializeOptions;

        public TwilioService(Microsoft.Extensions.Options.IOptions<TwilioAccountConfiguration> twilioAccountConfiguration,
            ILogger<TwilioService> log, IAwsStorageService awsStorageService)
        {
            _twilioAccountConfiguration = twilioAccountConfiguration.Value ?? throw new ArgumentException(nameof(twilioAccountConfiguration));
            TwilioClient.Init(_twilioAccountConfiguration.AccountSid, _twilioAccountConfiguration.AuthToken);
            _log = log;
            _awsStorageService = awsStorageService;
            _serializeOptions = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }

        public async Task<Room> CreateRoom(Room room)
        {
            var roomResource = await RoomResource.CreateAsync(
                uniqueName: room.Name,
                recordParticipantsOnConnect: room.IsRecordingEnabled,
                type: RoomResource.RoomTypeEnum.Group
                );

            room.SId = roomResource?.Sid;
            return room;
        }

        public async Task<RoomResource> GetRoom(string roomName)
        {
            var roomResource = await RoomResource.FetchAsync(pathSid: roomName);
            return roomResource;
        }

        public string GenerateToken(string roomName, TwilioIdentity identity)
        {
            var grant = new VideoGrant();
            grant.Room = roomName;
            var grants = new HashSet<IGrant> { grant };

            var stringIdentity = SerializeObject(identity);
            var token = new Token(_twilioAccountConfiguration.AccountSid, _twilioAccountConfiguration.ApiKeySid,
                _twilioAccountConfiguration.ApiKeySecret, identity: stringIdentity, grants: grants);

            return token.ToJwt();
        }

        public async Task<Result> EndRoom(Room room)
        {
            var openRooms = await GetRoomsByUniqueNameAndStatus(room.Name, RoomStatusEnum.InProgress);
            try
            {
                await Task.WhenAll(openRooms.Select(r => RoomResource.UpdateAsync(status: RoomStatusEnum.Completed, pathSid: r.Sid)));
                return Result.Ok();
            }
            catch (Exception e)
            {
                _log.LogError("Twilio room could not be closed.", e);
                return Result.Fail(new Error(e.Message));
            }
        }

        public async Task<CompositionResource> CreateComposition(string roomSid, string witnessEmail)
        {
            var witnessSid = await GetWitnessSid(roomSid, witnessEmail);

            var layout = new
            {
                grid = new
                {
                    video_sources = new string[] { witnessSid }
                }
            };
            var composition = await CompositionResource.CreateAsync(
              roomSid: roomSid,
              audioSources: new List<string> { "*" },
              videoLayout: layout,
              statusCallback: new Uri(_twilioAccountConfiguration.StatusCallbackUrl),
              format: FormatEnum.Mp4
            );

            return composition;
        }

        // Returns the Twilio Participant Id (SId), if not returns the first in the list
        //TODO Validate this approach
        private async Task<string> GetWitnessSid(string roomSid, string witnessEmail)
        {
            var participants = await GetParticipantsByRoom(roomSid);
            var witnessParticipant = participants.First(x => DeserializeObject(x.Identity).Email == witnessEmail);
            if (witnessParticipant == null)
                return participants.First().Sid;

            return witnessParticipant.Sid;
        }

        private async Task<List<ParticipantResource>> GetParticipantsByRoom(string roomSid)
        {
            var participants = await ParticipantResource.ReadAsync(roomSid);
            return participants.ToList();
        }

        private async Task<List<RoomResource>> GetRoomsByUniqueNameAndStatus(string uniqueName, RoomStatusEnum status = null)
        {
            var rooms = await RoomResource.ReadAsync(status: status, uniqueName: uniqueName);
            return rooms.ToList();
        }

        // TODO: PoC code, we can return and store some Download information into Composition Entity?
        // TODO: move this code to a FileService? 
        public async Task<bool> GetCompositionMediaAsync(Composition composition)
        {
            _log.LogDebug($"Downloading composition - SId: {composition.SId}");
            var request = (HttpWebRequest)WebRequest.Create($"https://video.twilio.com{composition.MediaUri}?Ttl=3600");

            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_twilioAccountConfiguration.ApiKeySid}:{_twilioAccountConfiguration.ApiKeySecret}")));
            request.AllowAutoRedirect = false;

            string responseBody = "";
            try
            {
                await request.GetResponseAsync();
            }
            catch (WebException e)
            {
                if (e.Message.Contains("302"))
                {
                    using var streamReader = new StreamReader(e.Response.GetResponseStream());
                    responseBody = await streamReader.ReadToEndAsync();
                }
            }

            var mediaLocation = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody)["redirect_to"];

            await new WebClient().DownloadFileTaskAsync(new Uri(mediaLocation), $"{composition.SId}.mp4");

            _log.LogDebug($"Composition downloaded - SId: {composition.SId}");

            return true;

        }

        // TODO: Return Result
        public async Task<bool> UploadCompositionMediaAsync(Composition composition)
        {
            var filePath = $"{composition.SId}.mp4";
            var file = new FileInfo(filePath);
            var keyName = $"videos/{composition.SId}.mp4";

            _log.LogDebug($"Uploading composition - SId: {composition.SId} - keyName: {keyName}");

            if (file.Exists)
            {
                var fileTransferInfo = new FileTransferInfo
                {
                    FileStream = file.OpenRead(),
                    Name = file.Name,
                    Length = file.Length,
                };

                var result = await _awsStorageService.UploadMultipartAsync(keyName, fileTransferInfo, _twilioAccountConfiguration.S3DestinationBucket);
                if (result.IsFailed)
                {
                    _log.LogDebug($"Upload failed. Deleting local file {filePath}");
                    file.Delete();
                    _log.LogDebug($"File {filePath} deleted");

                    return false;
                }

                _log.LogDebug($"Successful upload. Deleting local file {filePath}");
                file.Delete();
                _log.LogDebug($"File {filePath} deleted");
                return true;

            }
            _log.LogError($"File Not found - path: {filePath}");
            return false;
        }

        private string SerializeObject(object item)
        {
            return JsonConvert.SerializeObject(item, _serializeOptions);
        }

        private TwilioIdentity DeserializeObject(string item)
        {
            return (TwilioIdentity)JsonConvert.DeserializeObject(item, typeof(TwilioIdentity), _serializeOptions);
        }
        
        public async Task<Result> UploadCompositionMetadata(CompositionRecordingMetadata metadata)
        {
            try
            {
                var fileKeyName = $"videos/{metadata.Name}.json";
                string json = JsonConvert.SerializeObject(metadata);
                var file = new FileTransferInfo
                {
                    FileStream = new MemoryStream(Encoding.ASCII.GetBytes(json)),
                    Name = metadata.Name,
                    Length = json.Length
                };

                await _awsStorageService.UploadMultipartAsync(fileKeyName, file, _twilioAccountConfiguration.S3DestinationBucket);
            }
            catch (Exception e)
            {
                _log.LogError($"Error uploading composition metadata file : {e.Message}");
                return Result.Fail(new Error("Error uploading CompositionRecordingMetadata"));
            }

            return Result.Ok();
        }
    }
}
