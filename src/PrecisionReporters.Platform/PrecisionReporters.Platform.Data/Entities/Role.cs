using PrecisionReporters.Platform.Data.Enums;
using System;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class Role : BaseEntity<Role>
    {
        public RoleName Name { get; set; }

        public override void CopyFrom(Role entity)
        {
            Name = entity.Name;
        }
    }
}
