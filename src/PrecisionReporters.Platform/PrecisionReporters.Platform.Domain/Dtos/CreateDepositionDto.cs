using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class CreateDepositionDto
    {
        [Required]
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        [Required]
        public string TimeZone { get; set; }
        public string Caption { get; set; }
        public CreateParticipantDto Witness { get; set; }
        [Required]
        public bool IsVideoRecordingNeeded { get; set; }
        public string RequesterEmail { get; set; }
        [MaxLength(500)]
        public string Details { get; set; }
        public List<CreateParticipantDto> Participants { get; set; }
    }
}
