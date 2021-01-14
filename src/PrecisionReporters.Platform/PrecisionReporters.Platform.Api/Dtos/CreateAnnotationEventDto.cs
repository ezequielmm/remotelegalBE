using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class CreateAnnotationEventDto
    {
        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public AnnotationAction Action { get; set; }
        [Required]
        public string Details { get; set; }
    }
}
