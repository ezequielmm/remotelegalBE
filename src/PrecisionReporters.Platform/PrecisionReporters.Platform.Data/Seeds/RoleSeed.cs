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

            // Deposition Admin
            var depositionAdminRoleId = Guid.Parse("a11c8ce3-0a39-47a5-a276-6a6b90e40ba5");
            modelBuilder.Entity<Role>().HasData(new Role { Id = depositionAdminRoleId, Name = RoleName.DepositionAdmin });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = depositionAdminRoleId, Action = ResourceAction.Cancel });

            // Court reporter
            var courtReporterRoleId = Guid.Parse("6c73879b-cce3-47ea-9b80-12e1c4d1285e");
            modelBuilder.Entity<Role>().HasData(new Role { Id = courtReporterRoleId, Name = RoleName.DepositionCourtReporter });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = courtReporterRoleId, Action = ResourceAction.EndDeposition });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = courtReporterRoleId, Action = ResourceAction.Recording });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = courtReporterRoleId, Action = ResourceAction.ViewSharedDocument });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = courtReporterRoleId, Action = ResourceAction.StampExhibit });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = courtReporterRoleId, Action = ResourceAction.View });

            // deposition attendee
            var depositionAttendeeRoleId = Guid.Parse("997d199c-3b9a-4103-a320-130b02890a5b");
            modelBuilder.Entity<Role>().HasData(new Role { Id = depositionAttendeeRoleId, Name = RoleName.DepositionAttendee });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = depositionAttendeeRoleId, Action = ResourceAction.UploadDocument });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = depositionAttendeeRoleId, Action = ResourceAction.ViewSharedDocument });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = depositionAttendeeRoleId, Action = ResourceAction.View });

            //Document Owner
            var documentOwnerRoleId = Guid.Parse("ef7db7d6-4aae-11eb-b378-0242ac130002");
            modelBuilder.Entity<Role>().HasData(new Role { Id = documentOwnerRoleId, Name = RoleName.DocumentOwner });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = documentOwnerRoleId, Action = ResourceAction.Delete });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = documentOwnerRoleId, Action = ResourceAction.Update });
            modelBuilder.Entity<RolePermission>().HasData(new RolePermission { RoleId = documentOwnerRoleId, Action = ResourceAction.View });
        }
    }
}
