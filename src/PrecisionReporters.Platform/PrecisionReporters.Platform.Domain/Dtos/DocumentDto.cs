using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Data.Enums;
using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class DocumentDto
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public long Size { get; set; }
        public Guid Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public UserOutputDto AddedBy { get; set; }
        public DateTimeOffset? SharedAt { get; set; }
        public string StampLabel { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public DocumentType DocumentType { get; set; }
    }
}