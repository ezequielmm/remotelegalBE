using System;
using PrecisionReporters.Platform.Shared.Attributes;
using PrecisionReporters.Platform.Shared.Enums;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class MediaStampDto
    {
        public string StampLabel { get; set; }
        [ResourceId(ResourceType.Deposition)]
        public Guid DepositionId { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
    }
}