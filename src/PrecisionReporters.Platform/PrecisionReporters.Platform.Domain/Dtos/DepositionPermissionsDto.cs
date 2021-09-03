using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Shared.Enums;

namespace PrecisionReporters.Platform.Domain.Dtos
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