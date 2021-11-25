using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Twilio.Rest.Conversations.V1.Service;
using Twilio.Rest.Video.V1;
using static Twilio.Rest.Video.V1.RoomResource;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ITwilioService
    {
        Task<Room> CreateRoom(Room room, bool configureCallbacks);
        Task<RoomResource> GetRoom(string roomName);
        string GenerateToken(string roomName, TwilioIdentity identity, bool grantChat = false);
        Task<Result> EndRoom(Room room);
        Task<CompositionResource> CreateComposition(Room room, string[] witnessSid);
        Task<bool> GetCompositionMediaAsync(Composition composition);
        Task<bool> UploadCompositionMediaAsync(Composition composition);
        Task<Result> UploadCompositionMetadata(CompositionRecordingMetadata metadata);
        Task<Result> DeleteCompositionAndRecordings(DeleteTwilioRecordingsDto deleteTwilioRecordings);
        Task<Result<string>> CreateChat(string chatName);
        Task<Result<UserResource>> CreateChatUser(TwilioIdentity identity);
        Task<Result> AddUserToChat(string conversationSid, TwilioIdentity identity, string userSid);
        Task<List<RoomResource>> GetRoomsByUniqueNameAndStatus(string uniqueName, RoomStatusEnum status = null);
        Task<Result<DateTime>> GetVideoStartTimeStamp(string roomSid);
        Task<bool> RemoveRecordingRules(string roomSid);
        Task<bool> AddRecordingRules(string roomSid, TwilioIdentity witnessIdentity, bool IsVideoRecordingNeeded);
        Task<UserResource> GetExistingChatUser(TwilioIdentity identity);
        TwilioIdentity DeserializeObject(string item);
        Task<string[]> GetWitnessSid(string roomSid, string witnessEmail);
    }
}
