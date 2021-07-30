using PrecisionReporters.Platform.Data.Enums;
using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class ActivityHistoryDto
    {
        public Guid Id { get; set; }
        public DateTime ActivityDate { get; set; }
        public string Device { get; set; }
        public string Browser { get; set; }
        public ActivityHistoryAction Action { get; set; }
        public string OperatingSystem { get; set; }
    }
}
