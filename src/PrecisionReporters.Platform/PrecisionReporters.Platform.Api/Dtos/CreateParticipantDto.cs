﻿using System.ComponentModel.DataAnnotations;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class CreateParticipantDto
    {
        [MaxLength(50)]
        public string Name { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        [RegularExpression(@"^\(?([2-9][0-8][0-9])\)?[-. ]?([2-9][0-9]{2})[-. ]?([0-9]{4})$")]
        public string Phone { get; set; }
        [Required]
        //TODO: This property should be an enum
        public ParticipantRole Role { get; set; }
    }
}