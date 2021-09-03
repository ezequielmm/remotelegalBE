using System;
using MessagePack;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Shared.Attributes;

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
        [Key("sampleRate")] 
        public int SampleRate { get; set; }
    }
}