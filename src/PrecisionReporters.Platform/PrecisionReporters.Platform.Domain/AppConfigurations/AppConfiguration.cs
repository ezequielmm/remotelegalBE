﻿using PrecisionReporters.Platform.Domain.AppConfigurations;
using PrecisionReporters.Platform.Domain.AppConfigurations.Sections;
using PrecisionReporters.Platform.Domain.Configurations;

namespace PrecisionReporters.Platform.Domain
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
        public CorsConfiguration CorsConfiguration { get; set; }
        public DocumentConfiguration DocumentConfiguration { get; set; }
        public AwsStorageConfiguration AwsStorageConfiguration { get; set; }
        public GcpConfiguration GcpConfiguration { get; set; }
        public AzureCognitiveServiceConfiguration AzureCognitiveServiceConfiguration { get; set; }
        public CloudServicesConfiguration CloudServicesConfiguration { get; set; }
        public VerificationLinkConfiguration VerificationLinkConfiguration { get; set; }
        public DepositionConfiguration DepositionConfiguration { get; set; }
        public ReminderConfiguration ReminderConfiguration { get; set; }
        public LocalStackConfiguration LocalStackConfiguration { get; set; }
        public EmailTemplateNames EmailTemplateNames { get; set; }
    }
}
