using PrecisionReporters.Platform.Data.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    //TODO: Refactor the entity and add new BaseEntity without Id Field
    public class UserResourceRole
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreationDate { get; set; }
        [Key]
        [ForeignKey(nameof(Role))]
        [Column(TypeName = "char(36)")]
        public Guid RoleId { get; set; }

        public Role Role { get; set; }

        [Key]
        [ForeignKey(nameof(User))]
        [Column(TypeName = "char(36)")]
        public Guid UserId { get; set; }

        public User User { get; set; }

        [Key]
        [Column(TypeName = "char(36)")]
        public Guid ResourceId { get; set; }

        public ResourceType ResourceType { get; set; }

        public void CopyFrom(UserResourceRole entity)
        {
            Role = entity.Role;
            User = entity.User;
            ResourceId = entity.ResourceId;
            ResourceType = entity.ResourceType;
        }
    }
}
