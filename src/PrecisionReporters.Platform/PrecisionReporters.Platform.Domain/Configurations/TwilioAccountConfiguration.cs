﻿namespace PrecisionReporters.Platform.Domain.Configurations
{
    public class TwilioAccountConfiguration
    {
        public const string SectionName = "TwilioAccountConfiguration";
        public string AccountSid { get; set; }
        public string AuthToken { get; set; }
        public string ApiKeySid { get; set; }
        public string ApiKeySecret { get; set; }
    }
}
