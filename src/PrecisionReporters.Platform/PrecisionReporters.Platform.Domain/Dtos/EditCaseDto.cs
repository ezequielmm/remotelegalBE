using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class EditCaseDto
    {
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string CaseNumber { get; set; }
    }
}