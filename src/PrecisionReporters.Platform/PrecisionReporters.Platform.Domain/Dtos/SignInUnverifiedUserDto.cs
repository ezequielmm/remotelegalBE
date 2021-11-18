using System.ComponentModel.DataAnnotations;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class SignInUnverifiedUserDto
    {
        [Required]
        public string Password { get; set; }
        
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string EmailAddress { get; set; }
        
        [Required]
        public ParticipantType ParticipantType { get; set; }
        
        public string Device { get; set; }
        
        public string Browser { get; set; }
    }
}