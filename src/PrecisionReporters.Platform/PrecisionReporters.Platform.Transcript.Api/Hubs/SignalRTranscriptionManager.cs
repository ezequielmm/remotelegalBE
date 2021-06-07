using Microsoft.AspNetCore.SignalR;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Extensions;
using PrecisionReporters.Platform.Transcript.Api.Hubs.Interfaces;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Transcript.Api.Hubs
{
    public class SignalRTranscriptionManager : ISignalRTranscriptionManager
    {
        private readonly IHubContext<TranscriptionHub> _transcriptionHub;

        public SignalRTranscriptionManager(IHubContext<TranscriptionHub> transcriptionHub)
        {
            _transcriptionHub = transcriptionHub;
        }

        public async Task SendNotificationToDepositionMembers(Guid depositionId, NotificationDto notificationDto)
        {
            await _transcriptionHub.Clients.Group(depositionId.GetDepositionSignalRGroupName()).SendAsync(nameof(ITranscriptionClient.ReceiveNotification), notificationDto);
        }        

        public async Task SendDirectMessage(string userId, NotificationDto notificationDto)
        {
            await _transcriptionHub.Clients.User(userId).SendAsync(nameof(ITranscriptionClient.ReceiveNotification), notificationDto);
        }
    }
}
