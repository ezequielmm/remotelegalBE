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
using Amazon.Runtime;

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
            _log = log;
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

        // TODO: move this code to a FileService? 
        // TODO: PoC code, we can return and store some UploadResponse data into Composition Entity.
        public async Task<bool> UploadCompositionMediaAsync(Composition composition)
        {
            var filePath = $"{composition.SId}.mp4";
            var file = new FileInfo(filePath);
            var keyName = $"recordings/{composition.Room.Name}/{composition.SId}.mp4";

            _log.LogDebug($"Uploading composition - SId: {composition.SId} - keyName: {keyName}");

            if (file.Exists)
            {
                // Create list to store upload part responses.
                var uploadResponses = new List<UploadPartResponse>();

                // Setup information required to initiate the multipart upload.
                var initiateRequest = new InitiateMultipartUploadRequest
                {
                    BucketName = _twilioAccountConfiguration.S3DestinationBucket,
                    Key = keyName
                };

                // Initiate the upload.
                var initResponse =
                    await _fileTransferUtility.S3Client.InitiateMultipartUploadAsync(initiateRequest);

                // Upload parts
                var contentLength = file.Length;
                var partSize = 5 * (long)Math.Pow(2, 20); // 5 MB

                try
                {
                    _log.LogDebug("Uploading parts");

                    long filePosition = 0;
                    for (int i = 1; filePosition < contentLength; i++)
                    {
                        var uploadRequest = new UploadPartRequest
                        {
                            BucketName = _twilioAccountConfiguration.S3DestinationBucket,
                            Key = keyName,
                            UploadId = initResponse.UploadId,
                            PartNumber = i,
                            PartSize = partSize,
                            FilePosition = filePosition,
                            FilePath = filePath
                        };

                        // Track upload progress.
                        uploadRequest.StreamTransferProgress +=
                            new EventHandler<StreamTransferProgressArgs>(UploadPartProgressEventCallback);

                        // Upload a part and add the response to our list.
                        uploadResponses.Add(await _fileTransferUtility.S3Client.UploadPartAsync(uploadRequest));

                        filePosition += partSize;
                    }

                    // Setup to complete the upload.
                    var completeRequest = new CompleteMultipartUploadRequest
                    {
                        BucketName = _twilioAccountConfiguration.S3DestinationBucket,
                        Key = keyName,
                        UploadId = initResponse.UploadId
                    };
                    completeRequest.AddPartETags(uploadResponses);

                    // Complete the upload.
                    var completeUploadResponse =
                        await _fileTransferUtility.S3Client.CompleteMultipartUploadAsync(completeRequest);

                    _log.LogDebug($"File uploaded {composition.SId}");
                    file.Delete();
                    _log.LogDebug($"File deleted - {filePath}");
                }
                catch (Exception exception)
                {
                    _log.LogError("An AmazonS3Exception was thrown: {0}", exception.Message);

                    // Abort the upload.
                    var abortMPURequest = new AbortMultipartUploadRequest
                    {
                        BucketName = _twilioAccountConfiguration.S3DestinationBucket,
                        Key = keyName,
                        UploadId = initResponse.UploadId
                    };
                    await _fileTransferUtility.S3Client.AbortMultipartUploadAsync(abortMPURequest);

                    // Delete local file
                    if (file.Exists)
                    {
                        file.Delete();
                    }

                    return false;
                }
            }
            else
            {
                _log.LogError($"File Not found - path: {filePath}");
                return false;
            }
             
            return true;
        }

        private void UploadPartProgressEventCallback(object sender, StreamTransferProgressArgs e)
        {
            // Process event. 
            _log.LogDebug("{0}/{1}", e.TransferredBytes, e.TotalBytes);
        }
    }
}
