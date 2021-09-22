using PrecisionReporters.Platform.Domain.Dtos;

namespace PrecisionReporters.Platform.Domain.Handlers
{
    public abstract class HandlerSignalRNotificationBase<T> : HandlerBase<T>
    {
        public SnsNotificationDTO CreateMessage(Shared.Dtos.DocumentDto document, string message)
        {
            return new SnsNotificationDTO()
            {
                Message = message,
                Data = document.ResourceId
            };
        }
    }
}
