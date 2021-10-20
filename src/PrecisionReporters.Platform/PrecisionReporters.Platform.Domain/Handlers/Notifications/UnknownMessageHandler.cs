using Amazon.SimpleNotificationService.Util;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Domain.Handlers.Notifications.Interfaces;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Handlers.Notifications
{
    public class UnknownMessageHandler : HandlerBase<Message>, IUnknownMessageHandler
    {
        private readonly ILogger<UnknownMessageHandler> _logger;
        public UnknownMessageHandler(ILogger<UnknownMessageHandler> logger)
        {
            _logger = logger;
        }
        public override async Task HandleRequest(Message request)
        {
            var msg = $"Unknown Message - Fail to Process: {request}";
            _logger.LogError(msg);

            if (successor != null)
                await successor.HandleRequest(request);
        }
    }
}
