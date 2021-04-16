using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Metadata;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddActivityHistoryTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SId",
                table: "Users",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChatSid",
                table: "Depositions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreRoomId",
                table: "Depositions",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivityHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ActivityDate = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<string>(nullable: false),
                    Device = table.Column<string>(nullable: true),
                    Browser = table.Column<string>(nullable: true),
                    IPAddress = table.Column<string>(nullable: true),
                    DepositionId = table.Column<string>(nullable: false),
                    Action = table.Column<string>(nullable: false),
                    ActionDetails = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityHistories_Depositions_DepositionId",
                        column: x => x.DepositionId,
                        principalTable: "Depositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Depositions_PreRoomId",
                table: "Depositions",
                column: "PreRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityHistories_DepositionId",
                table: "ActivityHistories",
                column: "DepositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityHistories_UserId",
                table: "ActivityHistories",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Depositions_Rooms_PreRoomId",
                table: "Depositions",
                column: "PreRoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Depositions_Rooms_PreRoomId",
                table: "Depositions");

            migrationBuilder.DropTable(
                name: "ActivityHistories");

            migrationBuilder.DropIndex(
                name: "IX_Depositions_PreRoomId",
                table: "Depositions");

            migrationBuilder.DropColumn(
                name: "SId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ChatSid",
                table: "Depositions");

            migrationBuilder.DropColumn(
                name: "PreRoomId",
                table: "Depositions");
        }
    }
}
