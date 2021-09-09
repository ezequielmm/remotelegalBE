using Amazon.SimpleNotificationService.Util;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Handlers.Notifications.Interfaces;
using PrecisionReporters.Platform.Domain.Parsers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Dtos;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Handlers.Notifications
{
    public class LambdaExceptionHandler : HandlerSignalRNotificationBase<Message>, ILambdaExceptionHandler
    {
        private readonly ILogger<LambdaExceptionHandler> _logger;
        private readonly IUserRepository _userRepository;
        private readonly ISignalRDepositionManager _signalRDepositionManager;
        public LambdaExceptionHandler(ILogger<LambdaExceptionHandler> logger, IUserRepository userRepository, ISignalRDepositionManager signalRDepositionManager)
        {
            _logger = logger;
            _userRepository = userRepository;
            _signalRDepositionManager = signalRDepositionManager;
        }
        public override async Task HandleRequest(Message message)
        {
            var parseMessage = SnsMessageParser.ParseExceptionInLambda(message.MessageText);
            if (parseMessage != null)
            {
                var errorDocument = parseMessage.Context;
                var user = await _userRepository.GetById(errorDocument.Document.AddedBy);
                // Send Notification using SignalR
                var notificationMessage = $"Failed to process file {errorDocument.Document.DisplayName}";
                var notificationDto = new NotificationDto()
                {
                    Action = NotificationAction.Error,
                    EntityType = NotificationEntity.Exhibit,
                    Content = CreateMessage(errorDocument.Document, notificationMessage)
                };
                await _signalRDepositionManager.SendDirectMessage(user.EmailAddress, notificationDto);

                var msg = $"Lambda Process Exception - Document: {errorDocument.Document.DisplayName} - Message: {errorDocument.Error}";
                _logger.LogError(msg);
            }
            else if (successor != null)
                await successor.HandleRequest(message);
        }
    }
}
