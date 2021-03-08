using PrecisionReporters.Platform.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class AddParticipantDto
    {
        [EmailAddress]
        public string EmailAddress { get; set; }

        [Required]
        public ParticipantType ParticipantType { get; set; }
    }
}