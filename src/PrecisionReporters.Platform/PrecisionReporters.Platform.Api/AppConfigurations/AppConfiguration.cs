using PrecisionReporters.Platform.Api.AppConfigurations;
using PrecisionReporters.Platform.Api.AppConfigurations.Sections;
using PrecisionReporters.Platform.Domain.Configurations;

namespace PrecisionReporters.Platform.Api
{
    public class AppConfiguration : IAppConfiguration
    {
        public ConnectionStrings ConnectionStrings { get; set; }
        public ConfigurationFlags ConfigurationFlags { get; set; }
        public Swagger Swagger { get; set; }
        public TwilioAccountConfiguration TwilioAccountConfiguration { get; set; }
        public CognitoConfiguration CognitoConfiguration { get; set; }
        public EmailConfiguration EmailConfiguration { get; set; }
        public UrlPathConfiguration UrlPathConfiguration { get; set; }
    }
}
