using PrecisionReporters.Platform.Data.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class AddParticipantDto
    {
        [EmailAddress]
        public string EmailAddress { get; set; }

        [Required]
        public ParticipantType ParticipantType { get; set; }
    }
}