using PrecisionReporters.Platform.Shared.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    //TODO: Refactor the entity and add new BaseEntity without Id Field
    public class UserResourceRole : BaseEntity<UserResourceRole>
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public new DateTime CreationDate { get; set; }
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

        public override void CopyFrom(UserResourceRole entity)
        {
            Role = entity.Role;
            User = entity.User;
            ResourceId = entity.ResourceId;
            ResourceType = entity.ResourceType;
        }
    }
}
