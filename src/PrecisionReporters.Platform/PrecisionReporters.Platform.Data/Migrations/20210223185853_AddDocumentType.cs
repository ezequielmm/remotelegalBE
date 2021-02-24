using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddDocumentType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "Documents",
                nullable: false);

            migrationBuilder.Sql("UPDATE Documents doc LEFT JOIN Depositions dep on dep.CaptionId = doc.Id SET DocumentType = 0 WHERE dep.CaptionId IS NULL");
            migrationBuilder.Sql("UPDATE Documents doc INNER JOIN Depositions dep on dep.CaptionId = doc.Id SET DocumentType = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "Documents");
        }
    }
}
