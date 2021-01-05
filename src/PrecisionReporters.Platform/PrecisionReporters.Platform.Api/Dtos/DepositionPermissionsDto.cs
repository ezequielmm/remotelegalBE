using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class DepositionPermissionsDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ParticipantType Role { get; set; }
        public bool IsAdmin { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public List<ResourceAction> Permissions { get; set; }
    }
}