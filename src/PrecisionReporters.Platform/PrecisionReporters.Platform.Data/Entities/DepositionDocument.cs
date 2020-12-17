
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class DepositionDocument : BaseEntity<DepositionDocument>
    {
        [ForeignKey(nameof(Deposition))]
        [Column(TypeName = "char(36)")]
        public Guid DepositionId { get; set; }
        public Deposition Deposition { get; set; }
        [ForeignKey(nameof(Document))]
        [Column(TypeName = "char(36)")]
        public Guid DocumentId { get; set; }
        public Document Document { get; set; }

        public override void CopyFrom(DepositionDocument entity)
        {
            Deposition = entity.Deposition;
            Document = entity.Document;
        }
    }
}
