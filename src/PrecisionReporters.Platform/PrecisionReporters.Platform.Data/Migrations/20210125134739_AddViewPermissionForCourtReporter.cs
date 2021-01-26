using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddViewPermissionForCourtReporter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "6c73879b-cce3-47ea-9b80-12e1c4d1285e", "View" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "6c73879b-cce3-47ea-9b80-12e1c4d1285e", "View" });
        }
    }
}
