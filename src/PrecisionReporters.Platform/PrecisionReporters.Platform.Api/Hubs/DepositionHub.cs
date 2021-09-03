using FluentResults;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Api.Hubs.Interfaces;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Shared.Authorization.Attributes;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Extensions;
using PrecisionReporters.Platform.Shared.Helpers.Interfaces;
using System;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Api.Hubs
{
    [Authorize]
    [HubName("/depositionHub")]
    public class DepositionHub : Hub<IDepositionClient>
    {
        private readonly ILogger<DepositionHub> _logger;
        private readonly IDepositionService _depositionService;
        private readonly ILoggingHelper _loggingHelper;
        public DepositionHub(ILogger<DepositionHub> logger, IDepositionService depositionService, ILoggingHelper loggingHelper)
        {
            _logger = logger;
            _depositionService = depositionService;
            _loggingHelper = loggingHelper;
        }

        [UserAuthorize(ResourceType.Deposition, ResourceAction.View)]
        public async Task<Result> SubscribeToDeposition(SubscribeToDepositionDto dto)
        {
            return await _loggingHelper.ExecuteWithDeposition(dto.DepositionId, async () =>
            {
                try
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, dto.DepositionId.GetDepositionSignalRGroupName());
                    var currentUserParticipant = await _depositionService.GetUserParticipant(dto.DepositionId);
                    if (currentUserParticipant.IsSuccess && (currentUserParticipant.Value.User.IsAdmin || currentUserParticipant.Value.Role == ParticipantType.CourtReporter))
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, dto.DepositionId.GetDepositionSignalRAdminsGroupName());
                    }
                    return Result.Ok();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"There was an error subscribing to Deposition {dto.DepositionId}");
                    return Result.Fail($"Unable to add user to Group {ApplicationConstants.DepositionGroupName}{dto.DepositionId}.");
                }
            });
        }
    }
}
