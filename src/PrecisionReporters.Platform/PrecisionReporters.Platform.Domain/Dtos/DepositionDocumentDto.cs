using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class DepositionDocumentDto
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public Guid DocumentId { get; set; }
        public Guid DepositionId { get; set; }
        public string StampLabel { get; set; }
    }
}