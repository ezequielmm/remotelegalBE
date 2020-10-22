using System.Threading.Tasks;
using Twilio.Rest.Video.V1;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ITwilioService
    {
        Task<RoomResource> CreateRoom(string roomName);

        Task<RoomResource> GetRoom(string name);

        string GenerateToken(string roomName, string username);
    }
}
