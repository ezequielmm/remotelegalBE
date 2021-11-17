using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddTranscriptionsPostProcessingEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TwilioAudioRecordings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime(3)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(3)"),
                    RecordingSId = table.Column<string>(nullable: true),
                    RoomSId = table.Column<string>(nullable: true),
                    RecordingCreationDateTime = table.Column<DateTime>(type: "datetime(3)", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "time(3)", nullable: true),
                    FfProbeCreationDateTime = table.Column<DateTime>(type: "datetime(3)", nullable: true),
                    FfProbeDuration = table.Column<TimeSpan>(type: "time(3)", nullable: true),
                    FfProbeStartTime = table.Column<TimeSpan>(type: "time(3)", nullable: true),
                    TranscriptionStatus = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwilioAudioRecordings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AzureMediaServiceJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime(3)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(3)"),
                    JobId = table.Column<string>(nullable: true),
                    TwilioAudioRecordingId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzureMediaServiceJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzureMediaServiceJobs_TwilioAudioRecordings_TwilioAudioRecor~",
                        column: x => x.TwilioAudioRecordingId,
                        principalTable: "TwilioAudioRecordings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PostProcessTranscriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime(3)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(3)"),
                    Text = table.Column<string>(nullable: true),
                    TwilioAudioRecordingId = table.Column<string>(nullable: true),
                    TranscriptDateTime = table.Column<DateTime>(type: "datetime(3)", nullable: false),
                    TwilioAudioRecordingStartTime = table.Column<TimeSpan>(type: "time(3)", nullable: false),
                    CompositionStartTime = table.Column<TimeSpan>(type: "time(3)", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time(3)", nullable: false),
                    Confidence = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostProcessTranscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostProcessTranscriptions_TwilioAudioRecordings_TwilioAudioR~",
                        column: x => x.TwilioAudioRecordingId,
                        principalTable: "TwilioAudioRecordings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AzureMediaServiceJobs_TwilioAudioRecordingId",
                table: "AzureMediaServiceJobs",
                column: "TwilioAudioRecordingId");

            migrationBuilder.CreateIndex(
                name: "IX_PostProcessTranscriptions_TwilioAudioRecordingId",
                table: "PostProcessTranscriptions",
                column: "TwilioAudioRecordingId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AzureMediaServiceJobs");

            migrationBuilder.DropTable(
                name: "PostProcessTranscriptions");

            migrationBuilder.DropTable(
                name: "TwilioAudioRecordings");
        }
    }
}
