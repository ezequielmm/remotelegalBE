namespace PrecisionReporters.Platform.Domain.Configurations
{
    public class EmailConfiguration
    {
        public const string SectionName = "EmailConfiguration";

        public string Sender { get; set; }
        public string EmailNotification { get; set; }
    }
}
