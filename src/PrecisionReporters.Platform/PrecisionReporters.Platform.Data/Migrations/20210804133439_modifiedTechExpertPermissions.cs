using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class modifiedTechExpertPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "ee816afa-0399-472d-947f-73bfcb17775e", "AdmitParticipants" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "ee816afa-0399-472d-947f-73bfcb17775e", "EndDeposition" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "ee816afa-0399-472d-947f-73bfcb17775e", "Recording" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "ee816afa-0399-472d-947f-73bfcb17775e", "StampExhibit" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "ee816afa-0399-472d-947f-73bfcb17775e", "UploadDocument" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "ee816afa-0399-472d-947f-73bfcb17775e", "UploadDocument" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[,]
                {
                    { "ee816afa-0399-472d-947f-73bfcb17775e", "EndDeposition" },
                    { "ee816afa-0399-472d-947f-73bfcb17775e", "Recording" },
                    { "ee816afa-0399-472d-947f-73bfcb17775e", "StampExhibit" },
                    { "ee816afa-0399-472d-947f-73bfcb17775e", "AdmitParticipants" }
                });
        }
    }
}
