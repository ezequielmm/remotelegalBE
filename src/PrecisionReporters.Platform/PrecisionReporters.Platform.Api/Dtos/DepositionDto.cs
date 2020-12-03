using System;
using System.Collections.Generic;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class DepositionDto
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string TimeZone { get; set; }
        public DepositionDocumentDto Caption { get; set; }
        public ParticipantDto Witness { get; set; }
        public bool IsVideoRecordingNeeded { get; set; }
        public RequesterUserOutputDto Requester { get; set; }
        public List<ParticipantDto> Participants { get; set; }
        public string Details { get; set; }
        public RoomDto Room { get; set; }
        public List<DepositionDocumentDto> Documents { get; set; }
    }
}
