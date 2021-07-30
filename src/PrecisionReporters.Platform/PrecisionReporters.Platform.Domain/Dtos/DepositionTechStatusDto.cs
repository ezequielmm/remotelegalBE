using System.Collections.Generic;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class DepositionTechStatusDto
    {
        public string RoomId { get; set; }
        public bool IsVideoRecordingNeeded { get; set; }
        public bool IsRecording { get; set; }
        public string? SharingExhibit { get; set; }
        public List<ParticipantTechStatusDto> Participants { get; set; }
    }
}
