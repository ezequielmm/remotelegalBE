using System;
using System.Collections.Generic;
using System.Text;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class DeleteTwilioRecordingsDto
    {
        public string RoomSid { get; set; }
        public string CompositionSid { get; set; }
    }
}
