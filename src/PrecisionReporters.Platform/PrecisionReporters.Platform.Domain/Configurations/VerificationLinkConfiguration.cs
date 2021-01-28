using System;
using System.Collections.Generic;
using System.Text;

namespace PrecisionReporters.Platform.Domain.Configurations
{
    public class VerificationLinkConfiguration
    {
        public const string SectionName = "VerificationLinkConfiguration";
        public int ExpirationTime {get;set;}
    }
}
