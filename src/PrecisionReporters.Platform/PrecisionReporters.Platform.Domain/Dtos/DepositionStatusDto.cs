using System;
using System.Collections.Generic;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class DepositionStatusDto
    {
        public string TimeZone { get; set; }
        public bool IsOnTheRecord { get; set; }
        public bool IsSharing { get; internal set; }
        public IEnumerable<ParticipantDto> Participants { get; set; }
        public bool ShouldSendToPreDepo { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public string JobNumber { get; set; }
    }
}