using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class Document: BaseEntity<Document>
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public string FilePath { get; set; }
        public long Size { get; set; }
        public List<DocumentUserDeposition> DocumentUserDepositions { get; set; }

        [NotMapped]
        public string FileKey { get; set; }
        [ForeignKey(nameof(AddedBy))]
        public Guid AddedById { get; set; }
        public User AddedBy { get; set; }

        public override void CopyFrom(Document entity)
        {
            Name = entity.Name;
            Type = entity.Type;
            FilePath = entity.FilePath;
            AddedBy = entity.AddedBy;
            Size = entity.Size;
            DisplayName = entity.DisplayName;
        }
    }
}