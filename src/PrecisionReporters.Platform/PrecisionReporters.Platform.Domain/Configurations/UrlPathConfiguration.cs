namespace PrecisionReporters.Platform.Domain.Configurations
{
    public class UrlPathConfiguration
    {
        public const string SectionName = "UrlPathConfiguration";

        public string FrontendBaseUrl { get; set; }
        public string VerifyUserUrl { get; set; }
        public string ForgotPasswordUrl { get; set; }
    }
}
