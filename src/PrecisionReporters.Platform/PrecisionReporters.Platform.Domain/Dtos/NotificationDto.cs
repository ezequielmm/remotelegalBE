using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using PrecisionReporters.Platform.Data.Enums;
using System.ComponentModel.DataAnnotations;
using MessagePack;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    [MessagePackObject]
    public class NotificationDto
    {        
        [MessagePack.Key("content")]
        public object Content { get; set; }

        [MessagePack.Key("action")]
        [EnumDataType(typeof(NotificationAction))]
        [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public NotificationAction Action { get; set; }
       
        [MessagePack.Key("entityType")]
        [EnumDataType(typeof(NotificationEntity))]
        [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public NotificationEntity EntityType { get; set; }                
    }
}
