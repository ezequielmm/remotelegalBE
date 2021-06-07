using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ISignalRTranscriptionManager
    {
        Task SendNotificationToDepositionMembers(Guid depositionId, NotificationDto notificationDto);
        Task SendDirectMessage(string userId, NotificationDto notificationDto);
    }
}