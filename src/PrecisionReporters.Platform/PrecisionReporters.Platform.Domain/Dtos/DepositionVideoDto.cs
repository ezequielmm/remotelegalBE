namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class DepositionVideoDto
    {
        public string PublicUrl { get; set; }
        public int TotalTime { get; set; }
        public int OnTheRecordTime { get; set; }
        public int OffTheRecordTime { get; set; }
    }
}
