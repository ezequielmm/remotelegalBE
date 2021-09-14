using PrecisionReporters.Platform.Data.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class ActivityHistory : BaseEntity<ActivityHistory>
    {
        public DateTime ActivityDate { get; set; }
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }
        public User User { get; set; }
        public string Device { get; set; }
        public string Browser { get; set; }
        public string IPAddress { get; set; }
        [ForeignKey(nameof(Deposition))]
        public Guid DepositionId { get; set; }
        public Deposition Deposition { get; set; }
        public ActivityHistoryAction Action { get; set; }
        public string ActionDetails { get; set; }
        public string OperatingSystem { get; set; }
        public string AmazonAvailability { get; set; }
        public string ContainerId { get; set; }
        public override void CopyFrom(ActivityHistory entity)
        {
            ActivityDate = entity.ActivityDate;
            UserId = entity.UserId;
            User = entity.User;
            Device = entity.Device;
            Browser = entity.Browser;
            IPAddress = entity.IPAddress;
            DepositionId = entity.DepositionId;
            Deposition = entity.Deposition;
            Action = entity.Action;
            ActionDetails = entity.ActionDetails;
            OperatingSystem = entity.OperatingSystem;
            AmazonAvailability = entity.AmazonAvailability;
            ContainerId = entity.ContainerId;
        }
    }
}
