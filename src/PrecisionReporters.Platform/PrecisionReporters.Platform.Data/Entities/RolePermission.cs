using PrecisionReporters.Platform.Shared.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class RolePermission
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreationDate { get; set; }

        [ForeignKey(nameof(Role))]
        [Column(TypeName = "char(36)")]
        public Guid RoleId { get; set; }
        
        public Role Role { get; set; }

        public ResourceAction Action { get; set; }
    }
}
