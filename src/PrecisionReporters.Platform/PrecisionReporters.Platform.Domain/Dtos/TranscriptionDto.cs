using System;
using MessagePack;
using PrecisionReporters.Platform.Domain.Enums;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    [MessagePackObject]
    public class TranscriptionDto
    {
        [Key("id")]
        public Guid Id { get; set; }
        [Key("creationDate")]
        public DateTime CreationDate { get; set; }
        [Key("text")]
        public string Text { get; set; }
        [Key("userId")]
        public Guid UserId { get; set; }
        [Key("depositionId")]
        public Guid DepositionId { get; set; }
        [Key("transcriptDateTime")]
        public DateTimeOffset TranscriptDateTime { get; set; }
        [Key("userName")]
        public string UserName { get; set; }
        [Key("userEmail")]
        public string UserEmail { get; set; }
        [Key("postProcessed")]
        public bool PostProcessed { get; set; }
        [Key("status")]
        public TranscriptionStatus Status { get; set; }
    }
}
