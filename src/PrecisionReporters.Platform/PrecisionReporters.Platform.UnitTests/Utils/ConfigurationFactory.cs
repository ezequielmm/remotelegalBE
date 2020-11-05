﻿using PrecisionReporters.Platform.Domain.Configurations;

namespace PrecisionReporters.Platform.UnitTests.Utils
{
    public class ConfigurationFactory
    {
        public static UrlPathConfiguration GetUrlPathConfiguration() 
        {
            return new UrlPathConfiguration
            {
                VerifyUserUrl = "VerifyUserUrl"
            };
        }

        public static EmailConfiguration GetEmailConfiguration()
        {
            return new EmailConfiguration
            {
                Sender = "SenderTest",
            };
        }
    }
}
