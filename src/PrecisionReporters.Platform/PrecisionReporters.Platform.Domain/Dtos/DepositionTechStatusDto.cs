using System.Collections.Generic;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class DepositionTechStatusDto
    {
        public string RoomId { get; set; }
        public bool IsVideoRecordingNeeded { get; set; }
        public bool IsRecording { get; set; }
        public string? SharingExhibit { get; set; }
        public List<ParticipantDto> Participants { get; set; }
    }
}
