namespace PrecisionReporters.Platform.Domain.Configurations
{
    public class EmailConfiguration
    {
        public const string SectionName = "EmailConfiguration";

        public string Sender { get; set; }
        public string EmailNotification { get; set; }
        public string ImagesUrl { get; set; }
        public string LogoImageName { get; set; }
        public string PreDepositionLink { get; set; }
        public string JoinDepositionTemplate { get; set; }
    }
}
