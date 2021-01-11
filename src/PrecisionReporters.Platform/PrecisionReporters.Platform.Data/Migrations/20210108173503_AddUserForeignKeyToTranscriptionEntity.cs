using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddUserForeignKeyToTranscriptionEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GcpDateTime",
                table: "Transcriptions");

            migrationBuilder.AddColumn<DateTime>(
                name: "TranscriptDateTime",
                table: "Transcriptions",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Transcriptions_UserId",
                table: "Transcriptions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transcriptions_Users_UserId",
                table: "Transcriptions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transcriptions_Users_UserId",
                table: "Transcriptions");

            migrationBuilder.DropIndex(
                name: "IX_Transcriptions_UserId",
                table: "Transcriptions");

            migrationBuilder.DropColumn(
                name: "TranscriptDateTime",
                table: "Transcriptions");

            migrationBuilder.AddColumn<DateTime>(
                name: "GcpDateTime",
                table: "Transcriptions",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
