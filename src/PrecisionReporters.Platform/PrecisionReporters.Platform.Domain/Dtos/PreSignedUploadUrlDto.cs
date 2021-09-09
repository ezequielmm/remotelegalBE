using PrecisionReporters.Platform.Shared.Attributes;
using PrecisionReporters.Platform.Shared.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class PreSignedUploadUrlDto
    {
        [Required]
        [ResourceId(ResourceType.Deposition)]
        public Guid DepositionId { get; set; }
        [Required]
        public string FileName { get; set; }
        [Required]
        public string ResourceId { get; set; }
    }
}
