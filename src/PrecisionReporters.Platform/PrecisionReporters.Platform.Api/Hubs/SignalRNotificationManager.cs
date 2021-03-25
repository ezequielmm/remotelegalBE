﻿using Microsoft.AspNetCore.SignalR;
using PrecisionReporters.Platform.Api.Extensions;
using PrecisionReporters.Platform.Api.Hubs.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Hubs
{
    public class SignalRNotificationManager : ISignalRNotificationManager
    {
        private readonly IHubContext<DepositionHub> _depositionHub;

        public SignalRNotificationManager(IHubContext<DepositionHub> depositionHub)
        {
            _depositionHub = depositionHub;
        }

        public async Task SendNotificationToDepositionMembers(Guid depositionId, NotificationDto notificationDto)
        {
            await _depositionHub.Clients.Group(depositionId.GetDepositionSignalRGroupName()).SendAsync(nameof(IDepositionClient.ReceiveNotification), notificationDto);
        }

        public async Task SendNotificationToDepositionAdmins(Guid depositionId, NotificationDto notificationDto)
        {
            await _depositionHub.Clients.Group(depositionId.GetDepositionSignalRAdminsGroupName()).SendAsync(nameof(IDepositionClient.ReceiveNotification), notificationDto);
        }

        public async Task SendDirectMessage(string userId, NotificationDto notificationDto)
        {
            await _depositionHub.Clients.User(userId).SendAsync(nameof(IDepositionClient.ReceiveNotification), notificationDto);
        }
    }
}
