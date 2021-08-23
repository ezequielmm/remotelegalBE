using System;
using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class PreSignedUploadUrlDto
    {
        [Required]
        public Guid DepositionId { get; set; }
        [Required]
        public string FileName { get; set; }
    }
}
