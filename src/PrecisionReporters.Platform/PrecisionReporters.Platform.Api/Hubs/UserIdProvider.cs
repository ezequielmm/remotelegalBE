using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace PrecisionReporters.Platform.Api.Hubs
{
    public class UserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.Email)?.Value;
        }
    }
}
