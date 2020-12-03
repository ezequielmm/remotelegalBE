using System;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class DepositionDocumentDto
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public UserOutputDto AddedBy { get; set; }
    }
}