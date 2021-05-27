using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Transcript.Api.AppConfigurations.Sections;

namespace PrecisionReporters.Platform.Transcript.Api.AppConfigurations
{
    public interface IAppConfiguration
    {
        ConnectionStrings ConnectionStrings { get; set; }
        ConfigurationFlags ConfigurationFlags { get; set; }
        Swagger Swagger { get; set; }
        TwilioAccountConfiguration TwilioAccountConfiguration { get; set; }
        CognitoConfiguration CognitoConfiguration { get; set; }
        EmailConfiguration EmailConfiguration { get; set; }
        UrlPathConfiguration UrlPathConfiguration { get; set; }
        AzureCognitiveServiceConfiguration AzureCognitiveServiceConfiguration { get; set; }
    }
}
