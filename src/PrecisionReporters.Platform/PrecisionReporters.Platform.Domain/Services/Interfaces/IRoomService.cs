using PrecisionReporters.Platform.Data.Entities;
using System.Threading.Tasks;
using FluentResults;
using System.Collections.Generic;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IRoomService
    {
        Task<Result<Room>> Create(Room room);
        Task<Result<Room>> GetByName(string roomName);
        Task<Result<string>> GenerateRoomToken(string roomName, string identity);
        Task<Result<Room>> EndRoom(Room room);
        Task<Result<Room>> StartRoom(Room room);
        Task<Result<Room>> GetRoomBySId(string roomSid);
    }
}
