using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class AnnotationEventDto
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public UserOutputDto Author { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public AnnotationAction Action { get; set; }
        
        public string Details { get; set; }
    }
}
