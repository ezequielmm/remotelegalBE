using FluentResults;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Authorization.Attributes;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Extensions;
using PrecisionReporters.Platform.Transcript.Api.Hubs.Interfaces;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Transcript.Api.Hubs
{
    [Authorize]
    [HubName("/transcriptionHub")]
    public class TranscriptionHub : Hub<ITranscriptionClient>
    {
        private readonly ILogger<TranscriptionHub> _logger;

        public TranscriptionHub(ILogger<TranscriptionHub> logger)
        {
            _logger = logger;
        }

        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<Result> SubscribeToDeposition(SubscribeToDepositionDto dto)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, dto.DepositionId.GetDepositionSignalRGroupName());
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"There was an error subscribing to Deposition {dto.DepositionId}");
                return Result.Fail($"Unable to add user to Group {ApplicationConstants.DepositionGroupName}{dto.DepositionId}.");
            }
        }
    }
}
