using System;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class CaseDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Name { get; set; }
    }
}
