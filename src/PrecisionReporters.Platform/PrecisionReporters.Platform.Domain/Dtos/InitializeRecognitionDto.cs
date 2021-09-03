using System;
using MessagePack;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Shared.Attributes;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    [MessagePackObject]
    public class InitializeRecognitionDto
    {
        [Key("depositionId")]
        [ResourceId(ResourceType.Deposition)]
        public Guid DepositionId { get; set; }
        [Key("sampleRate")]
        public int SampleRate { get; set; }
    }
}