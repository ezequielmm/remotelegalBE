using System;
using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class Room : BaseEntity<Room>
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        public string SId { get; set; }

        [Required]
        public RoomStatus Status { get; set; } = RoomStatus.Created; 

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsRecordingEnabled { get; set; } = false;

        public Composition Composition { get; set; }

        public Room() {}

        public Room(string name, bool isRecordingEnabled = false)
        {
            Name = name;
            StartDate = DateTime.UtcNow;
            IsRecordingEnabled = isRecordingEnabled;
        }

        public override void CopyFrom(Room entity)
        {
            Name = entity.Name;
            SId = entity.SId;
            Status = entity.Status;
            StartDate = entity.StartDate;
            EndDate = entity.EndDate;
            IsRecordingEnabled = entity.IsRecordingEnabled;
        }
    }
}
