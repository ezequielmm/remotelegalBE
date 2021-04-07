using System.Collections.Generic;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class CompositionRecordingMetadata
    {
        public string Video { get; set; }
        public string Name { get; set; }
        public string TimeZone { get; set; }
        public string TimeZoneDescription { get; set; }
        public long StartDate { get; set; }
        public long EndDate { get; set; }
        public List<CompositionInterval> Intervals { get; set; }
    }
}
