namespace PrecisionReporters.Platform.Domain.Configurations
{
    public class ReminderConfiguration
    {
        public const string SectionName = "ReminderConfiguration";
        public int[] MinutesBefore { get; set; }
        public string DailyExecution { get; set; }
        public int ReminderRecurrency { get; set; }
    }
}
