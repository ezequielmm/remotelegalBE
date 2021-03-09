using FluentResults;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Api.Extensions;
using PrecisionReporters.Platform.Api.Hubs.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Hubs
{
    [HubName("/depositionHub")]
    public class DepositionHub: Hub<IDepositionClient>
    {
        private readonly ILogger<DepositionHub> _logger;
        public DepositionHub(ILogger<DepositionHub> logger)
        {
            _logger = logger;
        }
        
        public async Task<Result> SubscribeToDeposition(Guid depositionId)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, depositionId.GetDepositionSignalRGroupName());
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"There was an error subscribing to Deposition {depositionId}");
                return Result.Fail($"Unable to add user to Group {ApplicationConstants.DepositionGroupName}{depositionId}.");
            }            
        }       
    }
}
