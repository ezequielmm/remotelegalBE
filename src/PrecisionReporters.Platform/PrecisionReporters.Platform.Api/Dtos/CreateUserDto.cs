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

        [Required]
        [RegularExpression(@"^\(?([2-9][0-8][0-9])\)?[-. ]?([2-9][0-9]{2})[-. ]?([0-9]{4})$")]
        public string PhoneNumber { get; set; }

        [Required]
        [MaxLength(50)]
        public string CompanyName { get; set; }

        [Required]
        [MaxLength(150)]
        public string CompanyAddress { get; set; }
    }
}
