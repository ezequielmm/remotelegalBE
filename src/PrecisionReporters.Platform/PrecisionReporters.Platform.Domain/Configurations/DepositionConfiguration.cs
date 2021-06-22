namespace PrecisionReporters.Platform.Domain.Configurations
{
    public class DepositionConfiguration
    {
        public const string SectionName = "DepositionConfiguration";
        public string CancelAllowedOffsetSeconds { get; set; }
        public string MinimumReScheduleSeconds { get; set; }
    }
}
