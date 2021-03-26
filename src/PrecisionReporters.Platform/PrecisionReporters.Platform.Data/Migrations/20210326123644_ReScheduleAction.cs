using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class ReScheduleAction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "a11c8ce3-0a39-47a5-a276-6a6b90e40ba5", "ReSchedule" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "a11c8ce3-0a39-47a5-a276-6a6b90e40ba5", "ReSchedule" });
        }
    }
}
