using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddDocumentOwnerRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[] { "ef7db7d6-4aae-11eb-b378-0242ac130002", "DocumentOwner" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "ef7db7d6-4aae-11eb-b378-0242ac130002", "Delete" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "ef7db7d6-4aae-11eb-b378-0242ac130002", "Update" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "ef7db7d6-4aae-11eb-b378-0242ac130002", "View" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "ef7db7d6-4aae-11eb-b378-0242ac130002", "View" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "ef7db7d6-4aae-11eb-b378-0242ac130002", "Update" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "ef7db7d6-4aae-11eb-b378-0242ac130002", "Delete" });

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "ef7db7d6-4aae-11eb-b378-0242ac130002");
        }
    }
}
