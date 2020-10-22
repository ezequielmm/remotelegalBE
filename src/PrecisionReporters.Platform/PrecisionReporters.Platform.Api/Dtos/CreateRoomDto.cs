using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class CreateRoomDto
    {
        [Required]
        public string Name { get; set; }
    }
}
