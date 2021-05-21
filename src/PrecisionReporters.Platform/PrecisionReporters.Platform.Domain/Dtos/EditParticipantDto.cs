using System;
using System.ComponentModel.DataAnnotations;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class EditParticipantDto
    {
        [Required]
        public Guid Id { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        [RegularExpression(@"^\(?([2-9][0-8][0-9])\)?[-. ]?([2-9][0-9]{2})[-. ]?([0-9]{4})$", ErrorMessage = "Invalid US phone number format")]
        public string Phone { get; set; }
        [Required]
        public ParticipantType Role { get; set; }
    }
}