using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class BreakRoomAttendee
    {
        [Column(TypeName = "char(36)")]
        [ForeignKey(nameof(BreakRoom))]
        public Guid BreakRoomId { get; set; }
        public BreakRoom BreakRoom { get; set; }

        [Column(TypeName = "char(36)")]
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
