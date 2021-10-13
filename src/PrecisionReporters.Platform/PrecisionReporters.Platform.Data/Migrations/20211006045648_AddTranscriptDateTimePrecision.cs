using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddTranscriptDateTimePrecision : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "TranscriptDateTime",
                table: "Transcriptions",
                type: "datetime(3)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "TranscriptDateTime",
                table: "Transcriptions",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)");
        }
    }
}
