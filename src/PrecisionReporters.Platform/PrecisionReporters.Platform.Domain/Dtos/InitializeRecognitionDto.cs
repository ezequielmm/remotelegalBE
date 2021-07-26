using System;
using MessagePack;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Attributes;

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