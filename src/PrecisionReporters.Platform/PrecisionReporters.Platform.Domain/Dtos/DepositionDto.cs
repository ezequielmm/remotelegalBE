using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Domain.Dtos
{    
    public class DepositionDto
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public DateTimeOffset? CompleteDate { get; set; }
        public string TimeZone { get; set; }
        public DocumentDto Caption { get; set; }
        public ParticipantDto Witness { get; set; }
        public bool IsVideoRecordingNeeded { get; set; }
        public UserDto Requester { get; set; }
        public List<ParticipantDto> Participants { get; set; }
        public string Details { get; set; }
        public RoomDto Room { get; set; }
        public List<DepositionDocumentDto> Documents { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public DepositionStatus Status { get; set; }
        public Guid CaseId { get; set; }
        public string CaseName { get; internal set; }
        public string CaseNumber { get; internal set; }
        public bool IsOnTheRecord { get; set; }
        public DocumentDto SharingDocument { get; set; }
        public string Job { get; set; }
        public string RequesterNotes { get; set; }
        public UserDto AddedBy { get; set; }
        public UserDto EndedBy { get; set; }
    }
}
