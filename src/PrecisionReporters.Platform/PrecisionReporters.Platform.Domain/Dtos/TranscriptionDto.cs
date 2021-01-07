using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class TranscriptionDto
    {
        public string Transcript { get; set; }
        public DateTime TimeOffset { get; set; }
    }
}
