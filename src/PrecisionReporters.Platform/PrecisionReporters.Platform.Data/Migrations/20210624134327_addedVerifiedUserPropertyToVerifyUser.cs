using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class addedVerifiedUserPropertyToVerifyUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationDate",
                table: "VerifyUsers",
                nullable: true);

            migrationBuilder.Sql("UPDATE VerifyUsers SET VerificationDate = '2021-03-16 00:00:00.000' WHERE IsUsed = 1;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerificationDate",
                table: "VerifyUsers");
        }
    }
}
