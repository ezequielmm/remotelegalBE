using FluentResults;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Api.Authorization.Attributes;
using PrecisionReporters.Platform.Api.Extensions;
using PrecisionReporters.Platform.Api.Hubs.Interfaces;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Hubs
{
    [Authorize]
    [HubName("/depositionHub")]
    public class DepositionHub: Hub<IDepositionClient>
    {
        private readonly ILogger<DepositionHub> _logger;
        public DepositionHub(ILogger<DepositionHub> logger)
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
