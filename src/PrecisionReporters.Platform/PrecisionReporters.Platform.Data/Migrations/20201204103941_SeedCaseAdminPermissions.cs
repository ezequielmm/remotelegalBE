using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class SeedCaseAdminPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Custom SQL for inserting permissions for existing cases
            migrationBuilder.Sql("INSERT INTO UserResourceRoles (RoleId, UserId, ResourceId, ResourceType) " +
                "SELECT 'c7f87850-e176-4865-b26b-cedac420a0c8', AddedById, Id, 'Case' FROM Cases WHERE AddedById IS NOT NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
