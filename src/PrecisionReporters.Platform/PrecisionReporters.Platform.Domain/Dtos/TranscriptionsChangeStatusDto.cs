using System;
using MessagePack;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Attributes;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    [MessagePackObject]
    public class TranscriptionsChangeStatusDto
    {
        [Key("depositionId")]
        [ResourceId(ResourceType.Deposition)]
        public Guid DepositionId { get; set; }
        [Key("offRecord")]
        public bool OffRecord { get; set; }
    }
}