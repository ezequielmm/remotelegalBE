using System.ComponentModel;

namespace PrecisionReporters.Platform.Data.Enums
{
    public enum RoomStatusCallback
    {
        [Description("recording-completed")]
        RecordingCompleted,
        [Description("participant-connected")]
        ParticipantConnected,
        [Description("room-ended")]
        RoomEnded,
        [Description("recording-started")]
        RecordingStarted
    }
}