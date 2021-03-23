using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddRevertAction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "a11c8ce3-0a39-47a5-a276-6a6b90e40ba5", "Revert" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
