using System;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class TranscriptionDto
    {
        public Guid Id { get; set; }
        public DateTime CreationDate { get; set; }
        public string Text { get; set; }
        public Guid UserId { get; set; }
        public Guid DepositionId { get; set; }
        public DateTimeOffset TranscriptDateTime { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
    }
}
