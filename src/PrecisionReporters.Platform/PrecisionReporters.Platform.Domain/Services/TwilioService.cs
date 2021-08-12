using FluentResults;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Twilio;
using Twilio.Jwt.AccessToken;
using Twilio.Rest.Chat.V2.Service.User;
using Twilio.Rest.Conversations.V1.Service;
using Twilio.Rest.Video.V1;
using Twilio.Rest.Video.V1.Room;
using Twilio.Types;
using static Twilio.Rest.Video.V1.RoomResource;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TwilioService : ITwilioService
    {
        private readonly TwilioAccountConfiguration _twilioAccountConfiguration;
        private readonly ILogger<TwilioService> _log;
        private readonly IAwsStorageService _awsStorageService;
        private readonly JsonSerializerSettings _serializeOptions;

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

        public async Task<Room> CreateRoom(Room room, bool configureCallbacks)
        {
            var roomResource = await RoomResource.CreateAsync(
                uniqueName: room.Name,
                recordParticipantsOnConnect: false,
                type: RoomResource.RoomTypeEnum.Group,
                statusCallback: configureCallbacks ? new Uri($"{_twilioAccountConfiguration.StatusCallbackUrl}/recordings/addEvent") : null
                );

            room.SId = roomResource?.Sid;
            return room;
        }

        public async Task<RoomResource> GetRoom(string roomName)
        {
            var roomResource = await RoomResource.FetchAsync(pathSid: roomName);
            return roomResource;
        }

        public string GenerateToken(string roomName, TwilioIdentity identity, bool grantChat)
        {
            //TODO: Change identity from JSON string to User Email
            var stringIdentity = SerializeObject(identity);
            var grant = new VideoGrant();
            grant.Room = roomName;
            _log.LogInformation($"{nameof(TwilioService)}.{nameof(TwilioService.GenerateToken)} Generating Token Room Name: {roomName}, User Identity: {stringIdentity}");
            var grants = new HashSet<IGrant> { grant };
            if (grantChat)
                grants.Add(new ChatGrant { ServiceSid = _twilioAccountConfiguration.ConversationServiceId });

            var expirationOffset = Convert.ToInt32(_twilioAccountConfiguration.ClientTokenExpirationMinutes);
            var token = new Token(_twilioAccountConfiguration.AccountSid, _twilioAccountConfiguration.ApiKeySid,
                _twilioAccountConfiguration.ApiKeySecret, identity: stringIdentity, expiration: DateTime.UtcNow.AddMinutes(expirationOffset), grants: grants);

            _log.LogInformation($"{nameof(TwilioService)}.{nameof(TwilioService.GenerateToken)} Token: {token.ToJwt()}");
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

        public async Task<CompositionResource> CreateComposition(Room room, string witnessEmail)
        {
            var options = new CreateCompositionOptions(room.SId);
            options.AudioSources = new List<string> { "*" };
            options.StatusCallback = new Uri(_twilioAccountConfiguration.StatusCallbackUrl);
            options.Format = CompositionResource.FormatEnum.Mp4;
            options.Trim = true;            
            if (room.IsRecordingEnabled)
            {
                var witnessSid = await GetWitnessSid(room.SId, witnessEmail);
                var joinedWitnessSid = string.Join(",", witnessSid);
                _log.LogInformation($"{nameof(TwilioService)}.{nameof(TwilioService.CreateComposition)} - Create Composition Room Sid: {room?.SId} - Array Witness SID: {joinedWitnessSid}");
                options.VideoLayout = new
                {
                    grid = new
                    {
                        video_sources = witnessSid
                    }
                };
            }

            return await CompositionResource.CreateAsync(options);
        }

        private async Task<string[]> GetWitnessSid(string roomSid, string witnessEmail)
        {
            var participants = await GetParticipantsByRoom(roomSid);
            var witnessArray = participants.Where(x => DeserializeObject(x.Identity).Email == witnessEmail).Select(w => w.Sid).ToArray();
            if (!witnessArray.Any())
            {
                _log.LogError("There was an error finding a witness in array: {@0} from the room SId: {1}", witnessArray, roomSid);
                throw new Exception("No Witness is found");
            }
            return witnessArray;
        }

        private async Task<List<ParticipantResource>> GetParticipantsByRoom(string roomSid)
        {
            var participants = await ParticipantResource.ReadAsync(roomSid);
            var participantsSid = participants.Select(x => x.Sid).ToList();
            _log.LogInformation("RoomService.StartRoom Participant Id List: {@0}", participantsSid);

            return participants.ToList();
        }

        public async Task<List<RoomResource>> GetRoomsByUniqueNameAndStatus(string uniqueName, RoomStatusEnum status = null)
        {
            var rooms = await RoomResource.ReadAsync(status: status, uniqueName: uniqueName);
            return rooms.ToList();
        }

        // TODO: PoC code, we can return and store some Download information into Composition Entity?
        // TODO: move this code to a FileService? 
        public async Task<bool> GetCompositionMediaAsync(Composition composition)
        {
            _log.LogDebug("Downloading composition - SId: {0}", composition.SId);
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

            using (var client = new WebClient())
            {
                await client.DownloadFileTaskAsync(new Uri(mediaLocation), $"{composition.SId}.{ApplicationConstants.Mp4}");
            }

            _log.LogDebug("Composition downloaded - SId: {0}", composition.SId);

            return true;

        }

        // TODO: Return Result
        public async Task<bool> UploadCompositionMediaAsync(Composition composition)
        {
            var filePath = $"{composition.SId}.{ApplicationConstants.Mp4}";
            var file = new FileInfo(filePath);
            var keyName = $"videos/{composition.SId}.{ApplicationConstants.Mp4}";

            _log.LogDebug("Uploading composition - SId: {0} - keyName: {1}", composition.SId, keyName);

            if (file.Exists)
            {
                var result = await UploadMultipartAsync(file, keyName);

                if (result.IsFailed)
                {
                    _log.LogDebug("Upload failed. Deleting local file {0}", filePath);
                    file.Delete();
                    _log.LogDebug("File {0} deleted", filePath);

                    return false;
                }

                _log.LogDebug("Successful upload. Deleting local file {0}", filePath);
                file.Delete();
                _log.LogDebug("File {0} deleted", filePath);
                return true;

            }
            _log.LogError("File Not found - path: {0}", filePath);
            return false;
        }

        private async Task<Result> UploadMultipartAsync(FileInfo file, string keyName)
        {
            using (Stream fileStream = file.OpenRead())
            {
                var fileTransferInfo = new FileTransferInfo
                {
                    FileStream = fileStream,
                    Name = file.Name,
                    Length = file.Length,
                };

                return await _awsStorageService.UploadMultipartAsync(keyName, fileTransferInfo, _twilioAccountConfiguration.S3DestinationBucket);
            }
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
                _log.LogError(e, "Error uploading composition metadata file : {0}", e.Message);
                return Result.Fail(new Error("Error uploading CompositionRecordingMetadata"));
            }

            return Result.Ok();
        }

        public async Task<Result> DeleteCompositionAndRecordings(DeleteTwilioRecordingsDto deleteTwilioRecordings)
        {
            _log.LogDebug("Start method to Delete composition and recordings - Composition SId: {0}", deleteTwilioRecordings.CompositionSid);
            var recordings = await RoomRecordingResource.ReadAsync(deleteTwilioRecordings.RoomSid);
            foreach (var recording in recordings)
            {
                await RecordingResource.DeleteAsync(recording.Sid);
                _log.LogDebug("Deleted recording - SId: {0}", recording.Sid);
            }
            await CompositionResource.DeleteAsync(deleteTwilioRecordings.CompositionSid);
            _log.LogDebug("Deleted composition - SId: {0}", deleteTwilioRecordings.CompositionSid);
            return Result.Ok();
        }

        public async Task<Result<string>> CreateChat(string chatName)
        {
            try
            {
                var chatRoomResource = await ConversationResource.CreateAsync(
                    pathChatServiceSid: _twilioAccountConfiguration.ConversationServiceId,
                    friendlyName: chatName,
                    uniqueName: chatName
                );
                if (chatRoomResource != null)
                    return Result.Ok(chatRoomResource.Sid);

                _log.LogError("Error creating chat with name: {0}", chatName);
                return Result.Fail(new Error($"Error creating chat with name: {chatName}"));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error creating chat with name: {}", chatName);
                return Result.Fail(new Error($"Error creating chat with name: {chatName}"));
            }
        }

        public async Task<Result<string>> CreateChatUser(TwilioIdentity identity)
        {
            //TODO: Change identity from JSON string to User Email
            try
            {
                var strIdentity = SerializeObject(identity);
                var user = await UserResource.CreateAsync(
                    pathChatServiceSid: _twilioAccountConfiguration.ConversationServiceId,
                    identity: strIdentity);
                if (user != null)
                    return Result.Ok(user.Sid);

                _log.LogError("Error creating user with identity: {0}", identity.Email);
                return Result.Fail(new Error($"Error creating user with identity: {identity.Email}"));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error creating user with identity: {0}", identity.Email);
                return Result.Fail(new Error($"Error creating user with identity: {identity.Email}"));
            }

        }

        public async Task<Result> AddUserToChat(string conversationSid, TwilioIdentity identity, string userSid)
        {
            var strIdentity = SerializeObject(identity);
            UserChannelResource userChannel = null;
            if (!string.IsNullOrWhiteSpace(userSid))
            {
                try
                {
                    userChannel = await UserChannelResource.FetchAsync(_twilioAccountConfiguration.ConversationServiceId, strIdentity, conversationSid);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error getting user info: {0}", identity.Email);
                }
            }

            try
            {
                if (userChannel == null)
                {
                    var chatParticipant = await Twilio.Rest.Conversations.V1.Service.Conversation.ParticipantResource.CreateAsync(
                        pathChatServiceSid: _twilioAccountConfiguration.ConversationServiceId,
                        pathConversationSid: conversationSid,
                        identity: strIdentity);
                    return Result.Ok(chatParticipant?.Sid);
                }

                _log.LogError("Error adding user to chat, user identity: {0}", identity.Email);
                return Result.Fail(new Error($"Error adding user to chat, user identity: {identity.Email}"));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error adding user to chat, user identity: {0}", identity.Email);
                return Result.Fail(new Error($"Error adding user to chat, user identity: {identity.Email}"));
            }

        }

        public async Task<Result<DateTime>> GetVideoStartTimeStamp(string roomSid)
        {
            var recordings = await RoomRecordingResource.ReadAsync(roomSid);
            var firstRecording = recordings.OrderBy(x => x.DateCreated).First();
            var date = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(_twilioAccountConfiguration.TwilioStartedDateReference) + firstRecording.Offset.Value);
            return Result.Ok(date.UtcDateTime);
        }

        public async Task<bool> RemoveRecordingRules(string roomSid)
        {
            RoomResource room;
            try
            {
                room = await RoomResource.FetchAsync(pathSid: roomSid);
            }
            catch (Exception)
            {
                room = null;
            }
            if (room == null || room.Status == RoomStatusEnum.Completed)
                return true;

            await RecordingRulesResource.UpdateAsync(
                rules: new List<RecordingRule>(){
                        new RecordingRule(RecordingRule.TypeEnum.Exclude,true,null,null,null)
                },
                pathRoomSid: roomSid
            );
            return true;
        }

        public async Task<bool> AddRecordingRules(string roomSid, TwilioIdentity witnessIdentity, bool IsVideoRecordingNeeded)
        {
            var stringIdentity = SerializeObject(witnessIdentity);
            var rules = new List<RecordingRule>(){
                        new RecordingRule(RecordingRule.TypeEnum.Include,null,stringIdentity,null,null),
                        new RecordingRule(RecordingRule.TypeEnum.Include,null,null,null,RecordingRule.KindEnum.Audio)
            };

            if (IsVideoRecordingNeeded)
                rules.Add(new RecordingRule(RecordingRule.TypeEnum.Include, null, null, null, RecordingRule.KindEnum.Video));

            var recordingRules = await RecordingRulesResource.UpdateAsync(
                    rules: rules,
                    pathRoomSid: roomSid
                );
            return true;
        }

        public async Task<UserResource> GetExistingChatUser(TwilioIdentity identity)
        {
            var users = await UserResource.ReadAsync(_twilioAccountConfiguration.ConversationServiceId);
            var existingUser = users.FirstOrDefault(u => u.Identity == SerializeObject(identity));
            return existingUser;
        }
    }
}
