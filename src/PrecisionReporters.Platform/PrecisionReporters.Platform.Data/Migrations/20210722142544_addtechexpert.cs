using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class addtechexpert : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "6c73879b-cce3-47ea-9b80-12e1c4d1285e", "ViewDepositionStatus" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[] { "ee816afa-0399-472d-947f-73bfcb17775e", "DepositionTechExpert" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[,]
                {
                    { "ee816afa-0399-472d-947f-73bfcb17775e", "EndDeposition" },
                    { "ee816afa-0399-472d-947f-73bfcb17775e", "Recording" },
                    { "ee816afa-0399-472d-947f-73bfcb17775e", "ViewSharedDocument" },
                    { "ee816afa-0399-472d-947f-73bfcb17775e", "StampExhibit" },
                    { "ee816afa-0399-472d-947f-73bfcb17775e", "View" },
                    { "ee816afa-0399-472d-947f-73bfcb17775e", "AdmitParticipants" },
                    { "ee816afa-0399-472d-947f-73bfcb17775e", "ViewDepositionStatus" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "6c73879b-cce3-47ea-9b80-12e1c4d1285e", "ViewDepositionStatus" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "ee816afa-0399-472d-947f-73bfcb17775e", "View" });

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
                keyValues: new object[] { "ee816afa-0399-472d-947f-73bfcb17775e", "ViewSharedDocument" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "ee816afa-0399-472d-947f-73bfcb17775e", "StampExhibit" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "ee816afa-0399-472d-947f-73bfcb17775e", "AdmitParticipants" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "ee816afa-0399-472d-947f-73bfcb17775e", "ViewDepositionStatus" });

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "ee816afa-0399-472d-947f-73bfcb17775e");
        }
    }
}
