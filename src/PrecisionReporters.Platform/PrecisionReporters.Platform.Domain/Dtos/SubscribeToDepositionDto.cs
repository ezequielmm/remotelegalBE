using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Attributes;
using System;
using MessagePack;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    [MessagePackObject]
    public class SubscribeToDepositionDto
    {
        [Key("depositionId")]
        [ResourceId(ResourceType.Deposition)]
        public Guid DepositionId { get; set; }
    }
}
