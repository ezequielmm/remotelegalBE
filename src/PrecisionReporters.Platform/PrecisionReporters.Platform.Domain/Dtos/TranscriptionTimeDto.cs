using System;
namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class TranscriptionTimeDto
    {
        public string Text { get; set; }
        public DateTimeOffset TranscriptDateTime { get; set; }
        public int TranscriptionVideoTime { get; set; }
        public int Duration { get; set; }
        public double Confidence { get; set; }
        public string UserName { get; set; }
        public string Id { get; set; }
    }
}
