using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddDepositionAdminRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[] { "a11c8ce3-0a39-47a5-a276-6a6b90e40ba5", "DepositionAdmin" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "a11c8ce3-0a39-47a5-a276-6a6b90e40ba5", "Cancel" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "a11c8ce3-0a39-47a5-a276-6a6b90e40ba5", "Cancel" });

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "a11c8ce3-0a39-47a5-a276-6a6b90e40ba5");
        }
    }
}
