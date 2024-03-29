﻿namespace PrecisionReporters.Platform.Domain.Configurations
{
    public class EmailConfiguration
    {
        public const string SectionName = "EmailConfiguration";

        public string SesRegion { get; set; }
        public string Sender { get; set; }
        public string EmailNotification { get; set; }
        public string ImagesUrl { get; set; }
        public string LogoImageName { get; set; }
        public string CalendarImageName { get; set; }
        public string DepositionLink { get; set; }
        public string JoinDepositionTemplate { get; set; }
        public string SenderLabel { get; set; }
        public string XSesConfigurationSetHeader { get; set; }
    }
}
