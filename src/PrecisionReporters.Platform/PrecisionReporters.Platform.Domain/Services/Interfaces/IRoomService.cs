using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Threading.Tasks;

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
    }
}
