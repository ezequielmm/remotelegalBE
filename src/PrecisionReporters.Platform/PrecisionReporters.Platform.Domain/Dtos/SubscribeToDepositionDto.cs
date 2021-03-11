using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Attributes;
using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class SubscribeToDepositionDto
    {
        [ResourceId(ResourceType.Deposition)]
        public Guid DepositionId { get; set; }
    }
}
