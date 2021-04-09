using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class User : BaseEntity<User>
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [NotMapped]
        public string Password { get; set; }
        [Required]
        [MaxLength(255)]
        public string EmailAddress { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string CompanyName { get; set; }
        [Required]
        public string CompanyAddress { get; set; }
		public bool IsAdmin { get; set; }
        public List<DocumentUserDeposition> DocumentUserDepositions { get; set; }

        public virtual ICollection<Member> MemberOn { get; set; }

        public bool IsGuest { get; set; } = false;
        public string SId { get; set; }

        public override void CopyFrom(User entity)
        {
            FirstName = entity.FirstName;
            LastName = entity.LastName;
            EmailAddress = entity.EmailAddress;
            PhoneNumber = entity.PhoneNumber;
            Password = entity.Password;
            CreationDate = entity.CreationDate;
            CompanyName = entity.CompanyName;
            CompanyAddress = entity.CompanyAddress;
			IsAdmin = entity.IsAdmin;
            IsGuest = entity.IsGuest;
        }

        public string GetFullName()
        {
            return $"{FirstName} {LastName}";
        }
    }
}
