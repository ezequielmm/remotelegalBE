using System;

namespace PrecisionReporters.Platform.Domain.Handlers.Notifications
{
    public class NotificationHandlerException : Exception
    {
        public NotificationHandlerException(string message) : base(message)
        { }

        public NotificationHandlerException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}
