using PrecisionReporters.Platform.Data.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class DepositionEvent : BaseEntity<DepositionEvent>
    {
        public EventType EventType { get; set; }

        public User User { get; set; }

        public string Details { get; set; }

        public Deposition Deposition { get; set; }
        [ForeignKey(nameof(Deposition))]
        public Guid DepositionId { get; set; }

        public override void CopyFrom(DepositionEvent entity)
        {
            EventType = entity.EventType;
            User.CopyFrom(entity.User);
            Details = entity.Details;
        }
    }
}
