using PrecisionReporters.Platform.Data.Enums;
namespace PrecisionReporters.Platform.Data.Entities
{
    public class VerifyUser : BaseEntity<VerifyUser>
    {
        public bool IsUsed { get; set; }

        public User User { get; set; }
        public VerificationType VerificationType { get; set; }

        public override void CopyFrom(VerifyUser entity)
        {
            IsUsed = entity.IsUsed;
            VerificationType = entity.VerificationType;
            CreationDate = entity.CreationDate;
            User.CopyFrom(entity.User);
        }
    }
}
