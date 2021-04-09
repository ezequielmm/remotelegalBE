using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore.Internal;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class Deposition : BaseEntity<Deposition>
    {
        [Required]
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? CompleteDate { get; set; }
        [Required]
        public string TimeZone { get; set; }
        public string Details { get; set; }
        public bool IsVideoRecordingNeeded { get; set; }
        [NotMapped]
        public string FileKey { get; set; }
        public List<Participant> Participants { get; set; }
        public List<DepositionDocument> Documents { get; set; }
        public List<DocumentUserDeposition> DocumentUserDepositions { get; set; }
        [Required]
        public DepositionStatus Status { get; set; } = DepositionStatus.Pending;

        [ForeignKey(nameof(Caption))]
        public Guid? CaptionId { get; set; }
        public Document Caption { get; set; }
        [ForeignKey(nameof(Requester))]
        public Guid RequesterId { get; set; }
        public User Requester { get; set; }
        [ForeignKey(nameof(Room))]
        public Guid? RoomId { get; set; }
        public Room Room { get; set; }
        [ForeignKey(nameof(PreRoom))]
        public Guid? PreRoomId { get; set; }
        public Room PreRoom { get; set; }
        [ForeignKey(nameof(Case))]
        public Guid CaseId { get; set; }
        public Case Case { get; set; }
        [ForeignKey(nameof(SharingDocument))]
        public Guid? SharingDocumentId { get; set; }
        public Document SharingDocument { get; set; }

        public List<DepositionEvent> Events { get; set; }

        public bool IsOnTheRecord { get; set; } = false;

        [ForeignKey(nameof(AddedBy))]
        [Column(TypeName = "char(36)")]
        public Guid AddedById { get; set; }

        public User AddedBy { get; set; }

        public List<BreakRoom> BreakRooms { get; set; } = new List<BreakRoom>();
        [Column(TypeName = "varchar(50)")]
        public string Job { get; set; }
        public string RequesterNotes { get; set; }
        [ForeignKey(nameof(EndedBy))]
        [Column(TypeName = "char(36)")]
        public Guid? EndedById { get; set; }
        public User EndedBy { get; set; }
        public override void CopyFrom(Deposition entity)
        {
            StartDate = entity.StartDate;
            EndDate = entity.EndDate;
            CompleteDate = entity.CompleteDate;
            TimeZone = entity.TimeZone;
            Caption = entity.Caption;
            Details = entity.Details;
            Requester = entity.Requester;
            Participants = entity.Participants;
            Documents = entity.Documents;
            Status = entity.Status;
            Events = entity.Events;
            IsOnTheRecord = entity.IsOnTheRecord;
            BreakRooms = entity.BreakRooms;
            Job = entity.Job;
            RequesterNotes = entity.RequesterNotes;
            IsVideoRecordingNeeded = entity.IsVideoRecordingNeeded;
        }

        public DateTime? GetActualStartDate()
        {
            return this.Events != null && this.Events.Any(x => x.EventType == Enums.EventType.OnTheRecord)
                    ? this.Events.OrderBy(x => x.CreationDate).First(x => x.EventType == Enums.EventType.OnTheRecord).CreationDate
                    : (DateTime?)null;
        }
    }
}
