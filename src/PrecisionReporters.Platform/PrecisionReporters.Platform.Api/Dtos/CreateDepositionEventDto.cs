using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class CreateDepositionEventDto
    {
        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public EventType EventType { get; set; }
        public string Details { get; set; }
    }
}
