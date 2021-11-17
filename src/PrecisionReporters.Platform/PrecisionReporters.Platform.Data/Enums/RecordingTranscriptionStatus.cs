using System.ComponentModel;

namespace PrecisionReporters.Platform.Data.Enums
{
    public enum RecordingTranscriptionStatus
    {
        [Description("Unknown")]
        Unknown = 0,

        [Description("Pending")]
        Pending = 1,

        [Description("Completed")]
        Completed = 2
    }
}
