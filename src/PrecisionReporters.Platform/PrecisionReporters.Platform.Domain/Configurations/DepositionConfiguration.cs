using System;
using System.Collections.Generic;
using System.Text;

namespace PrecisionReporters.Platform.Domain.Configurations
{
    public class DepositionConfiguration
    {
        public const string SectionName = "DepositionConfiguration";
        public string CancelAllowedOffsetSeconds { get; set; }
        public string MinimumReScheduleSeconds { get; set; }
    }
}
