using System.ComponentModel.DataAnnotations;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class User : BaseEntity<User>
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        [MaxLength(255)]
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }

        public override void CopyFrom(User entity)
        {
            FirstName = entity.FirstName;
            LastName = entity.LastName;
            EmailAddress = entity.EmailAddress;
            PhoneNumber = entity.PhoneNumber;
            Password = entity.Password;
            CreationDate = entity.CreationDate;
        }
    }
}
