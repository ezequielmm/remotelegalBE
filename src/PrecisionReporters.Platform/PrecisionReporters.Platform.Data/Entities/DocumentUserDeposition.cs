using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class DocumentUserDeposition : BaseEntity<DocumentUserDeposition>
    {
        [ForeignKey(nameof(User))]
        [Column(TypeName = "char(36)")]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(Entities.Document))]
        [Column(TypeName = "char(36)")]
        public Guid DocumentId { get; set; }

        [ForeignKey(nameof(Deposition))]
        [Column(TypeName = "char(36)")]
        public Guid DepositionId { get; set; }

        public User User { get; set; }
        public Document Document {get;set;}
        public Deposition Deposition { get; set; }

        public override void CopyFrom(DocumentUserDeposition entity)
        {
            User.CopyFrom(entity.User);
            Document.CopyFrom(entity.Document);
            Deposition.CopyFrom(entity.Deposition);
        }
    }
}