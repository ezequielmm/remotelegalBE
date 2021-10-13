using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class Transcription : BaseEntity<Transcription>
    {
        [Required]
        public string Text { get; set; }
        [Column(TypeName = "char(36)")]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }
        [Column(TypeName = "char(36)")]
        public Guid DepositionId { get; set; }
        [Column(TypeName = "datetime(3)")]
        public DateTime TranscriptDateTime { get; set; }
        public User User { get; set; }
        public int Duration { get; set; }
        public double Confidence { get; set; }
        public bool PostProcessed { get; set; }

        public override void CopyFrom(Transcription entity)
        {
            Id = entity.Id;
            CreationDate = entity.CreationDate;
            Text = entity.Text;
            UserId = entity.UserId;
            DepositionId = entity.DepositionId;
            TranscriptDateTime = entity.TranscriptDateTime;
            Duration = entity.Duration;
            Confidence = entity.Confidence;
            PostProcessed = entity.PostProcessed;
        }
    }
}
