﻿namespace PrecisionReporters.Platform.Domain.Configurations
{
    public class CognitoConfiguration
    {
        public const string SectionName = "CognitoConfiguration";

        public string AWSRegion { get; set; }
        public string AWSAccessKey { get; set; }
        public string AWSSecretAccessKey { get; set; }
        public string ClientId { get; set; }
        public string UserPoolId { get; set; }
        public string Authority { get; set; }
        public string GuestClientId { get; set; }
        public string GuestUsersGroup { get; set; }
        public string GuestUsersPass { get; set; }
        public string UnVerifiedClientId { get; set; }
        public string UnVerifiedUsersGroup { get; set; }
    }
}
