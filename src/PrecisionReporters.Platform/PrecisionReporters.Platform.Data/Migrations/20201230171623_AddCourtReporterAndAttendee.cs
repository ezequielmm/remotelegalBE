using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddCourtReporterAndAttendee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[] { "6c73879b-cce3-47ea-9b80-12e1c4d1285e", "DepositionCourtReporter" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[] { "997d199c-3b9a-4103-a320-130b02890a5b", "DepositionAttendee" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "6c73879b-cce3-47ea-9b80-12e1c4d1285e", "EndDeposition" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "6c73879b-cce3-47ea-9b80-12e1c4d1285e", "Recording" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "997d199c-3b9a-4103-a320-130b02890a5b", "UploadDocument" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "6c73879b-cce3-47ea-9b80-12e1c4d1285e", "EndDeposition" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "6c73879b-cce3-47ea-9b80-12e1c4d1285e", "Recording" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "997d199c-3b9a-4103-a320-130b02890a5b", "UploadDocument" });

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "6c73879b-cce3-47ea-9b80-12e1c4d1285e");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "997d199c-3b9a-4103-a320-130b02890a5b");
        }
    }
}
