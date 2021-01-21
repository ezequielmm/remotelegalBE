using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class BreakRoom : BaseEntity<BreakRoom>
    {
        public string Name { get; set; }
        public bool IsLocked { get; set; } = false;

        [Column(TypeName = "char(36)")]
        [ForeignKey(nameof(Room))]
        public Guid RoomId { get; set; }

        public Room Room { get; set; }

        public List<BreakRoomAttendee> Attendees { get; set; } = new List<BreakRoomAttendee>();

        public Guid DepositionId { get; set; }

        public override void CopyFrom(BreakRoom entity) 
        {
            Name = entity.Name;
            IsLocked = entity.IsLocked;
            Room = entity.Room;
            Attendees = entity.Attendees;
        }
    }
}
