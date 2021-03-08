using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class CreateRoomDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public bool IsRecordingEnabled { get; set; }
    }
}
