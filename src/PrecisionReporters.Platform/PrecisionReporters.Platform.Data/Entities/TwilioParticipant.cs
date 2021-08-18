using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class TwilioParticipant : BaseEntity<TwilioParticipant>
    {
        public string ParticipantSid { get; set; }

        [ForeignKey(nameof(Participant))]
        public Guid? ParticipantId { get; set; }
        public Participant Participant { get; set; }
        public DateTime JoinTime { get; set; }
        public DateTime? DisconnectTime { get; set; }

        public override void CopyFrom(TwilioParticipant entity)
        {
            ParticipantId = entity.ParticipantId;
            ParticipantSid = entity.ParticipantSid;
            Participant = entity.Participant;
            JoinTime = entity.JoinTime;
            DisconnectTime = entity.DisconnectTime;
        }
    }
}
