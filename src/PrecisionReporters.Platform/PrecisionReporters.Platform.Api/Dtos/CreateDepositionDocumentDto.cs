using System;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class CreateDepositionDocumentDto
    {
        public Guid DocumentId { get; set; }
        public Guid DepositionId { get; set; }
    }
}