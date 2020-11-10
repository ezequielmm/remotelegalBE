using PrecisionReporters.Platform.Data.Entities;
using System.Threading.Tasks;
using FluentResults;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IRoomService
    {
        Task<Result<Room>> Create(Room room);
        Task<Result<Room>> GetByName(string roomName);
        Result<string> GenerateRoomToken(string roomName);
    }
}
