using PrecisionReporters.Platform.Data.Entities;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IRoomService
    {
        Task<Room> Create(Room room);
        Task<Room> GetByName(string roomName);
        string GenerateRoomToken(string roomName);
    }
}
