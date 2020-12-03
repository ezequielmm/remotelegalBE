using Microsoft.EntityFrameworkCore;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using System;

namespace PrecisionReporters.Platform.Data.Seeds
{
    public static class RoleSeed
    {
        public static void SeedRoles(this ModelBuilder modelBuilder)
        {
            // Case Admin
            var caseAdminRoleId = Guid.Parse("c7f87850-e176-4865-b26b-cedac420a0c8");
            modelBuilder.Entity<Role>().HasData(new Role { Id = caseAdminRoleId, Name = RoleName.CaseAdmin });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = caseAdminRoleId, Action = ResourceAction.Delete });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = caseAdminRoleId, Action = ResourceAction.Update });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = caseAdminRoleId, Action = ResourceAction.View });
        }
    }
}
