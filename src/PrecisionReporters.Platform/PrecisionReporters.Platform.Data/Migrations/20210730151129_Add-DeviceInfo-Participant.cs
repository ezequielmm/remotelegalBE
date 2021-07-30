using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Metadata;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddDeviceInfoParticipant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceInfoId",
                table: "Participants",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DevicesInfo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CameraName = table.Column<string>(nullable: true),
                    CameraStatus = table.Column<string>(nullable: false),
                    MicrophoneName = table.Column<string>(nullable: true),
                    SpeakersName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevicesInfo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Participants_DeviceInfoId",
                table: "Participants",
                column: "DeviceInfoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Participants_DevicesInfo_DeviceInfoId",
                table: "Participants",
                column: "DeviceInfoId",
                principalTable: "DevicesInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Participants_DevicesInfo_DeviceInfoId",
                table: "Participants");

            migrationBuilder.DropTable(
                name: "DevicesInfo");

            migrationBuilder.DropIndex(
                name: "IX_Participants_DeviceInfoId",
                table: "Participants");

            migrationBuilder.DropColumn(
                name: "DeviceInfoId",
                table: "Participants");
        }
    }
}
