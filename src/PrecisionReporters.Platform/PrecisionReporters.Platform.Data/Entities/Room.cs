using System;
using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class Room : BaseEntity<Room>
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        public override void CopyFrom(Room entity)
        {
            throw new NotImplementedException();
        }
    }
}
