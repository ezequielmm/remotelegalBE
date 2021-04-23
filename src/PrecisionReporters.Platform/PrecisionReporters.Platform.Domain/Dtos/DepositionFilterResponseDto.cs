using System.Collections.Generic;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class DepositionFilterResponseDto
    {
        public int TotalUpcoming { get; set; }
        public int TotalPast { get; set; }
        public int Page { get; set; }
        public int NumberOfPages { get; set; }
        public List<DepositionDto> Depositions { get; set;}
    }
}