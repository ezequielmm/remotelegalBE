﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class Deposition : BaseEntity<Deposition>
    {
        [Required]
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [Required]
        public string TimeZone { get; set; }
        public string Details { get; set; }
        public bool IsVideoRecordingNeeded { get; set; }
        [NotMapped]
        public string FileKey { get; set; }
        public List<Participant> Participants { get; set; }
        public List<DepositionDocument> Documents { get; set; }

        [ForeignKey(nameof(Caption))]
        public Guid? CaptionId { get; set; }
        public DepositionDocument Caption { get; set; }
        [ForeignKey(nameof(Witness))]
        public Guid? WitnessId { get; set; }
        public Participant Witness { get; set; }
        [ForeignKey(nameof(Requester))]
        public Guid RequesterId { get; set; }
        public User Requester { get; set; }
        [ForeignKey(nameof(Room))]
        public Guid? RoomId { get; set; }
        public Room Room { get; set; }

        public override void CopyFrom(Deposition entity)
        {
            StartDate = entity.StartDate;
            EndDate = entity.EndDate;
            TimeZone = entity.TimeZone;
            Caption = entity.Caption;
            Details = entity.Details;
            Witness = entity.Witness;
            Requester = entity.Requester;
            Participants = entity.Participants;
            Documents = entity.Documents;
        }
    }
}
