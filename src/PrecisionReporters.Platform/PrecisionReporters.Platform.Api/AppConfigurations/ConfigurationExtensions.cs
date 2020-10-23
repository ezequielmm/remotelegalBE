using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PrecisionReporters.Platform.Api
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
