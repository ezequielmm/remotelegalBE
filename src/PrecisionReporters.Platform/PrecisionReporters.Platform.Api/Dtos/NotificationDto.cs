using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Data.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class NotificationDto
    {
        public bool IsSystemMessage { get; set; } = false;
        public Guid? ActionUserId { get; set; }
        public DateTimeOffset ActionTimestamp { get; set; }
        public object Content { get; set; }

        [EnumDataType(typeof(NotificationAction))]
        [JsonConverter(typeof(StringEnumConverter))]
        public NotificationAction Action { get; set; }
       
        [EnumDataType(typeof(NotificationEntity))]
        [JsonConverter(typeof(StringEnumConverter))]
        public NotificationEntity EntityType { get; set; }                
    }
}
