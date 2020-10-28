using System;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class VerifyUser : BaseEntity<VerifyUser>
    {
        public bool IsUsed { get; set; }

        public User User { get; set; }

        public override void CopyFrom(VerifyUser entity)
        {
            IsUsed = entity.IsUsed;
            CreationDate = entity.CreationDate;
            User.CopyFrom(entity.User);
        }
    }
}
