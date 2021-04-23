using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class CreateGuestDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string EmailAddress { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ParticipantType ParticipantType { get; set; }
        public string Device { get; set; }
        public string Browser { get; set; }
    }
}
