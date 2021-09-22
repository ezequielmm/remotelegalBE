using PrecisionReporters.Platform.Domain.Dtos;
using System;

namespace PrecisionReporters.Platform.Domain.Handlers
{
    public abstract class HandlerSignalRNotificationBase<T> : HandlerBase<T>
    {
        public SnsNotificationDTO CreateMessage(Shared.Dtos.DocumentDto document, string message, Guid? documentId = null)
        {
            return new SnsNotificationDTO()
            {
                Message = message,
                Data = new UploadedExhibitDto
                {
                    ResourceId = document.ResourceId,
                    DocumentId = documentId
                }
            };
        }
    }
}
