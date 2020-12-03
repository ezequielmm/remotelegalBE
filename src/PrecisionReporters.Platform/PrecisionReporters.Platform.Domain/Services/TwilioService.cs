using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Domain.Commons;
using Twilio;
using Twilio.Jwt.AccessToken;
using Twilio.Rest.Video.V1;
using Twilio.Rest.Video.V1.Room;
using static Twilio.Rest.Video.V1.CompositionResource;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TwilioService : ITwilioService
    {
        private readonly TwilioAccountConfiguration _twilioAccountConfiguration;
        private readonly ILogger<TwilioService> _log;
        private readonly IAwsStorageService _awsStorageService;

        public TwilioService(Microsoft.Extensions.Options.IOptions<TwilioAccountConfiguration> twilioAccountConfiguration,
            ILogger<TwilioService> log, IAwsStorageService awsStorageService)
        {
            _twilioAccountConfiguration = twilioAccountConfiguration.Value ?? throw new ArgumentException(nameof(twilioAccountConfiguration));
            TwilioClient.Init(_twilioAccountConfiguration.AccountSid, _twilioAccountConfiguration.AuthToken);
            _log = log;
            _awsStorageService = awsStorageService;
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

        // TODO: Return Result
        public async Task<bool> UploadCompositionMediaAsync(Composition composition)
        {
            var filePath = $"{composition.SId}.mp4";
            var file = new FileInfo(filePath);
            var keyName = $"recordings/{composition.Room.Name}/{composition.SId}.mp4";

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
    }
}
