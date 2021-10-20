using FluentResults;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Domain.Handlers.Notifications.Interfaces;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Domain.Wrappers.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class SnsNotificationService : ISnsNotificationService
    {
        private readonly IAwsSnsWrapper _awsSnsWrapper;
        private readonly IMessageSignatureHandler _handler;
        private readonly ILogger<SnsNotificationService> _logger;

        public SnsNotificationService(ILogger<SnsNotificationService> logger, IAwsSnsWrapper awsSnsWrapper, ILambdaExceptionHandler lambdaExceptionHandler, IExhibitNotificationHandler exhibitNotificationHandler, ISubscriptionMessageHandler subscriptionMessageHandler, IMessageSignatureHandler messageSignatureHandler, IUnknownMessageHandler unknownMessageHandler)
        {
            _logger = logger;
            _awsSnsWrapper = awsSnsWrapper;
            lambdaExceptionHandler.SetSuccessor(unknownMessageHandler);
            exhibitNotificationHandler.SetSuccessor(lambdaExceptionHandler);
            subscriptionMessageHandler.SetSuccessor(exhibitNotificationHandler);
            _handler = messageSignatureHandler;
            _handler.SetSuccessor(subscriptionMessageHandler);
        }

        public async Task<Result<string>> Notify(Stream message)
        {
            try
            {
                _logger.LogInformation($"Init Exhibit Notification");
                using var reader = new StreamReader(message);
                var rawMessage = await reader.ReadToEndAsync();
                var awsMessage = _awsSnsWrapper.ParseMessage(rawMessage);
                await _handler.HandleRequest(awsMessage);
                _logger.LogInformation($"Exhibit Notification '{awsMessage.MessageId}' finished");
                return Result.Ok(awsMessage.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ExhibitNotificationService Error: {ex}");
                return Result.Fail(ex.Message);
            }
        }
    }
}
