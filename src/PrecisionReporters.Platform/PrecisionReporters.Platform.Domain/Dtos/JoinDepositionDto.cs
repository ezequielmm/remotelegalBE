﻿using System.Collections.Generic;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class JoinDepositionDto
    {
        public string Token { get; set; }
        public string TimeZone { get; set; }
        public bool IsOnTheRecord { get; set; }
        public bool IsSharing { get; internal set; }
        public List<ParticipantDto> Participants { get; set; }
    }
}
