using Microsoft.Extensions.Configuration;

namespace PrecisionReporters.Platform.Transcript.Api
{
    public static class ConfigurationExtensions
    {
        public static AppConfiguration GetApplicationConfig(this IConfiguration configuration)
        {
            var appConfig = new AppConfiguration();
            configuration.GetSection("AppConfiguration").Bind(appConfig);
            return appConfig;
        }
    }
}
