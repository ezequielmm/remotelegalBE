using System;
using System.Collections.Generic;
using System.Text;

namespace PrecisionReporters.Platform.Domain.Configurations
{
    public class EmailConfiguration
    {
        public const string SectionName = "EmailConfiguration";

        public string Sender { get; set; }
        public string EmailHelp { get; set; }
        public string VerifyEmailSubject { get; set; }
        public string BaseTemplatePath { get; set; }
        public string VerifyTemplateName { get; set; }
    }
}
