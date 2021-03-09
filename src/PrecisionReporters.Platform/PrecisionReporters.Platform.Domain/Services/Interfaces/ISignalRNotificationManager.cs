using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ISignalRNotificationManager
    {
        Task SendNotificationToGroupMembers(Guid depositionId, NotificationDto notificationDto);
    }
}