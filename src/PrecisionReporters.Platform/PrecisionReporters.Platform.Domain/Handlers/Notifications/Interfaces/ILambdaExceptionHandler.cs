using Amazon.SimpleNotificationService.Util;
using PrecisionReporters.Platform.Domain.Handlers.Interfaces;

namespace PrecisionReporters.Platform.Domain.Handlers.Notifications.Interfaces
{
    public interface ILambdaExceptionHandler : IHandlerBase<Message>
    {
    }
}
