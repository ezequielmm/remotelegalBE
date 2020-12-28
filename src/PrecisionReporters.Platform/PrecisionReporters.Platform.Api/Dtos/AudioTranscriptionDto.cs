using System;

namespace PrecisionReporters.Platform.Api.Dtos
{
    [Serializable]
    public class AudioTranscriptionDto
    {
        public string User { get; set; }
        public string AudioText { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public bool IsFinished { get; set; }
    }
}
