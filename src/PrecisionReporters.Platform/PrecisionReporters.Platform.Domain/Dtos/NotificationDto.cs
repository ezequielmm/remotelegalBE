using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class NotificationDto
    {        
        public object Content { get; set; }

        [EnumDataType(typeof(NotificationAction))]
        [JsonConverter(typeof(StringEnumConverter))]
        public NotificationAction Action { get; set; }
       
        [EnumDataType(typeof(NotificationEntity))]
        [JsonConverter(typeof(StringEnumConverter))]
        public NotificationEntity EntityType { get; set; }                
    }
}
