using System;
using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class CreateDepositionDto
    {
        [Required]
        public  DateTimeOffset StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        [Required]
        public string TimeZone { get; set; }
        public string Caption { get; set; }
        public CreateParticipantDto Witness { get; set; }
        [Required]
        public bool IsVideoRecordingNeeded { get; set; }
        [Required]
        public string RequesterEmail { get; set; }
        [MaxLength(500)]
        public string Details { get; set; }
    }
}
