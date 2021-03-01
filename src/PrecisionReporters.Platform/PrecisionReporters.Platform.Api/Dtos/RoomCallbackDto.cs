using System;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class RoomCallbackDto
    {
        public string RoomSid { get; set; }
        public string RecordingSid { get; set; }
        public string ParticipantSid { get; set; }
        public string StatusCallbackEvent { get; set; }
        public string Url { get; set; }
        public string RoomName { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public int Duration { get; set; }
    }
}
