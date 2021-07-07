using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Twilio.Rest.Video.V1;
using static Twilio.Rest.Video.V1.RoomResource;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IRoomService
    {
        Task<Result<Room>> Create(Room room);
        Task<Result<Room>> GetById(Guid roomId);
        Task<Result<Room>> GetByName(string roomName);
        Task<Result<string>> GenerateRoomToken(string roomName, User user, ParticipantType role, string email, ChatDto chatDto = null);
        Task<Result<Room>> EndRoom(Room room, string witnessEmail);
        Task<Result<Room>> StartRoom(Room room, bool configureCallbacks);
        Task<Result<Room>> GetRoomBySId(string roomSid);
        Task<Result<Room>> Update(Room room);
        Task<Result<Composition>> CreateComposition(Room room, string witnessEmail);
        Task<List<RoomResource>> GetTwilioRoomByNameAndStatus(string uniqueName, RoomStatusEnum status);
        Task<bool> RemoveRecordingRules(string roomSid);
        Task<bool> AddRecordingRules(string roomSid, TwilioIdentity witnessIdentity, bool IsVideoRecordingNeeded);
    }
}
