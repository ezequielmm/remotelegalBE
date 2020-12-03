
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class DepositionDocument : BaseEntity<DepositionDocument>
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string FilePath { get; set; }
        [NotMapped]
        public string FileKey { get; set; }
        [ForeignKey(nameof(AddedBy))]
        public Guid AddedById { get; set; }
        public User AddedBy { get; set; }

        public override void CopyFrom(DepositionDocument entity)
        {
            Name = entity.Name;
            Type = entity.Type;
            FilePath = entity.FilePath;
            AddedBy = entity.AddedBy;
        }
    }
}
