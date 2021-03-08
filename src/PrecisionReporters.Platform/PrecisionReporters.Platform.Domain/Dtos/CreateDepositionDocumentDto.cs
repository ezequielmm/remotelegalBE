using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class CreateDepositionDocumentDto
    {
        public Guid DocumentId { get; set; }
        public Guid DepositionId { get; set; }
    }
}