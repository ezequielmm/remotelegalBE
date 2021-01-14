using System;
using System.ComponentModel.DataAnnotations.Schema;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class AnnotationEvent : BaseEntity<AnnotationEvent>
    {
        [ForeignKey(nameof(User))]
        [Column(TypeName = "char(36)")]
        public Guid AuthorId { get; set; }
        public User Author { get; set; }

        public AnnotationAction Action { get; set; }

        [ForeignKey(nameof(Document))]
        [Column(TypeName = "char(36)")]
        public Guid DocumentId { get; set; }

        public Document Document { get; set; }

        public string Details { get; set; }

        public override void CopyFrom(AnnotationEvent entity)
        {
            throw new NotImplementedException();
        }
    }
}
