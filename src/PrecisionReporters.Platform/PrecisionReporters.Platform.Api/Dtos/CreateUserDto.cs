using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class CreateUserDto
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string EmailAddress { get; set; }

        [RegularExpression(@"^((\+[0-9]|)(\d{3})(?:[\).\s]?)(\d{3})(?:[-\.\s]?)(\d{4})(?!\d)|)$")]
        public string PhoneNumber { get; set; }
    }
}
