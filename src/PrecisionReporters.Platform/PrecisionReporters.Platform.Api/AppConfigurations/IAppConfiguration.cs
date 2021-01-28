using PrecisionReporters.Platform.Api.AppConfigurations.Sections;
using PrecisionReporters.Platform.Domain.Configurations;

namespace PrecisionReporters.Platform.Api.AppConfigurations
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
