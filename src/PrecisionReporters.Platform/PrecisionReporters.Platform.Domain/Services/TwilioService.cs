using Amazon.S3.Transfer;
using Newtonsoft.Json;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Twilio;
using Twilio.Jwt.AccessToken;
using Twilio.Rest.Video.V1;
using static Twilio.Rest.Video.V1.CompositionResource;
using Twilio.Rest.Video.V1.Room;
using System.Linq;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TwilioService : ITwilioService
    {
        private readonly TwilioAccountConfiguration _twilioAccountConfiguration;
        private static ITransferUtility _fileTransferUtility;
        private readonly ILogger<TwilioService> _log;

        public TwilioService(Microsoft.Extensions.Options.IOptions<TwilioAccountConfiguration> twilioAccountConfiguration,
            ITransferUtility fileTransferUtility, ILogger<TwilioService> log)
        {
            _twilioAccountConfiguration = twilioAccountConfiguration.Value ?? throw new ArgumentException(nameof(twilioAccountConfiguration));
            TwilioClient.Init(_twilioAccountConfiguration.AccountSid, _twilioAccountConfiguration.AuthToken);
            _fileTransferUtility = fileTransferUtility;
        }

        public async Task<RoomResource> CreateRoom(Room room)
        {
            var roomResource = await RoomResource.CreateAsync(
                uniqueName: room.Name,
                recordParticipantsOnConnect: room.IsRecordingEnabled,
                type: RoomResource.RoomTypeEnum.Group
                );
            return roomResource;
        }

        public async Task<RoomResource> GetRoom(string name)
        {
            var roomResource = await RoomResource.FetchAsync(pathSid: name);
            return roomResource;
        }

        public string GenerateToken(string roomName, string username)
        {
            var grant = new VideoGrant();
            grant.Room = roomName;
            var grants = new HashSet<IGrant> { grant };
            var token = new Token(_twilioAccountConfiguration.AccountSid, _twilioAccountConfiguration.ApiKeySid,
                _twilioAccountConfiguration.ApiKeySecret, identity: username.ToString(), grants: grants);

            return token.ToJwt();
        }

        public async Task<RoomResource> EndRoom(string roomSid)
        {
            var room = await RoomResource.UpdateAsync(
                status: RoomResource.RoomStatusEnum.Completed,
                pathSid: roomSid
            );

            return room;
        }

        public async Task<CompositionResource> CreateComposition(RoomResource room)
        {
            var participants = await GetParticipantsByRoom(room);

            var layout = new
            {
                grid = new
                {
                    video_sources = new string[] { participants.First().Sid }
                }
            };
            var composition = await CompositionResource.CreateAsync(
              roomSid: room.Sid,
              audioSources: new List<string> { "*" },
              videoLayout: layout,
              statusCallback: new Uri(_twilioAccountConfiguration.StatusCallbackUrl),
              format: FormatEnum.Mp4
            );

            return composition;
        }

        private async Task<List<ParticipantResource>> GetParticipantsByRoom(RoomResource room)
        {
            var participants = await ParticipantResource.ReadAsync(room.Sid);
            return participants.ToList();
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

            WebResponse response;
            string responseBody = "";
            try
            {
                response = request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Message.Contains("302"))
                {
                    response = e.Response;
                    responseBody = new StreamReader(response.GetResponseStream()).ReadToEnd();
                }
            }

            var mediaLocation = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody)["redirect_to"];

            new WebClient().DownloadFileAsync(new Uri(mediaLocation), $"{composition.SId}.mp4");

            _log.LogDebug($"Composition downloaded - SId: {composition.SId}");

            return true;

        }

        // TODO: PoC code, we can return and store some UploadResponse data into Composition Entity.
        public async Task<bool> UploadCompositionMediaAsync(Composition composition)
        {
            var filePath = $"{composition.SId}.mp4";
            var file = new FileInfo(filePath);
            var keyName = $"recordings/{composition.Room.Name}/{composition.SId}.mp4";

            _log.LogDebug($"Uploading composition - SId: {composition.SId} - keyName: {keyName}");

            try
            {
                if (file.Exists)
                {
                    PutObjectRequest request = new PutObjectRequest
                    {
                        BucketName = _twilioAccountConfiguration.S3DestinationBucket,
                        Key = keyName
                    };

                    using (FileStream stream = new FileStream(filePath, FileMode.Open))
                    {
                        request.InputStream = stream;
                        PutObjectResponse response = await _fileTransferUtility.S3Client.PutObjectAsync(request);
                    }

                    // TODO: this location must be temporal and deleted after uploading is completed,
                    // TODO: move this code to a FileService? 

                    _log.LogDebug($"File uploaded {composition.SId}");
                    file.Delete();
                    _log.LogDebug($"File deleted - {filePath}");
                }
                else
                {
                    _log.LogError($"File Not found - path: {filePath}");
                }
                
            }
            catch (Exception e)
            {
                _log.LogError(e, $"There was an error uploading the composition SId: {composition.SId}");
                
                if (file.Exists)
                {
                    file.Delete();
                }

                return false;
            }
            return true;
        }
    }
}
