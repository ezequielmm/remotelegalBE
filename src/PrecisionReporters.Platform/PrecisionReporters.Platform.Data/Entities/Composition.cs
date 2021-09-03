using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class Composition : BaseEntity<Composition>
    {
        public string SId { get; set; }

        public string Url { get; set; }

        public string MediaUri { get; set; }

        public CompositionStatus Status { get; set; } = CompositionStatus.Queued;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime? LastUpdated { get; set; }

        [Column(TypeName = "char(36)")]
        public Guid RoomId { get; set; }

        public Room Room { get; set; }

        [Column(TypeName = "char(5)")]
        public string FileType { get; set; }

        public int RecordDuration { get; set; }
        public Composition() { }

        public override void CopyFrom(Composition entity)
        {
            SId = entity.SId;
            Url = entity.Url;
            Status = entity.Status;
            StartDate = entity.StartDate;
            EndDate = entity.EndDate;
            LastUpdated = entity.LastUpdated;
            RoomId = entity.RoomId;
            FileType = entity.FileType;
        }
    }
}
