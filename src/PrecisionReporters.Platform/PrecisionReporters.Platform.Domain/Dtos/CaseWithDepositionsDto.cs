using System;
using System.Collections.Generic;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class CaseWithDepositionsDto
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string Name { get; set; }
        public string CaseNumber { get; set; }
        public Guid AddedById { get; set; }
        public List<DepositionDto> Depositions { get; set; }
    }
}