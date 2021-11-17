using System;
using System.Collections.Generic;
using System.Text;

namespace PrecisionReporters.Platform.Domain.Configurations
{
    public class EmailTemplateNames
    {
        public const string SectionName = "EmailTemplateNames";
        public string VerificationEmail { get; set; }
        public string ForgotPasswordEmail { get; set; }
        public string DownloadCertifiedTranscriptEmail { get; set; }
        public string DownloadAssetsEmail { get; set; }
        public string JoinDepositionEmail { get; set; }
        public string ActivityEmail { get; set; }
        public string CancelDepositionEmail { get; set; }
        public string ReScheduleDepositionEmail { get; set; }
        public string DepositionReminderEmail { get; set; }
    }
}
