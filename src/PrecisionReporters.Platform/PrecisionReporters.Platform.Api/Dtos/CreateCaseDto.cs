using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class CreateCaseDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string CaseNumber { get; set; }
    }
}
