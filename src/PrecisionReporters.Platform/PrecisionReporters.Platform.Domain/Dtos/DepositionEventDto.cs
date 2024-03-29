﻿using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class DepositionEventDto
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public EventType EventType { get; set; }
        public UserOutputDto User { get; set; }
        public string Details { get; set; }
    }
}
