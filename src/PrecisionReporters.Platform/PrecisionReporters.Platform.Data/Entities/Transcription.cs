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
        public DateTime TranscriptDateTime { get; set; }
        public User User { get; set; }

        public override void CopyFrom(Transcription entity)
        {            
            CreationDate = entity.CreationDate;
        }
    }
}
