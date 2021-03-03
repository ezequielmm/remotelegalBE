using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class DraftTranscriptDto
    {
        public Guid DepositionId { get; set; }
        public Guid CurrentUserId { get; set; }
    }
}
