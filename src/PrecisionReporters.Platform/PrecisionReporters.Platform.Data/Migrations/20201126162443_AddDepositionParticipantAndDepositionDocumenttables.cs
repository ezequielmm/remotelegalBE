using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddDepositionParticipantAndDepositionDocumenttables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE Cases DROP FOREIGN KEY FK_Cases_Users_AddedById;");

            migrationBuilder.AlterColumn<string>(
                name: "AddedById",
                table: "Cases",
                type: "char(36)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(36)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Depositions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    StartDate = table.Column<DateTime>(nullable: false),
                    EndDate = table.Column<DateTime>(nullable: true),
                    TimeZone = table.Column<string>(nullable: false),
                    Details = table.Column<string>(nullable: true),
                    IsVideoRecordingNeeded = table.Column<bool>(nullable: false),
                    CaptionId = table.Column<string>(nullable: true),
                    WitnessId = table.Column<string>(nullable: true),
                    RequesterId = table.Column<string>(nullable: false),
                    RoomId = table.Column<string>(nullable: true),
                    CaseId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Depositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Depositions_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Depositions_Users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Depositions_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepositionDocuments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    FilePath = table.Column<string>(nullable: true),
                    AddedById = table.Column<string>(nullable: false),
                    DepositionId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositionDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepositionDocuments_Users_AddedById",
                        column: x => x.AddedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepositionDocuments_Depositions_DepositionId",
                        column: x => x.DepositionId,
                        principalTable: "Depositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Role = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    Phone = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: true),
                    DepositionId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Participants_Depositions_DepositionId",
                        column: x => x.DepositionId,
                        principalTable: "Depositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Participants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepositionDocuments_AddedById",
                table: "DepositionDocuments",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_DepositionDocuments_DepositionId",
                table: "DepositionDocuments",
                column: "DepositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Depositions_CaptionId",
                table: "Depositions",
                column: "CaptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Depositions_CaseId",
                table: "Depositions",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Depositions_RequesterId",
                table: "Depositions",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Depositions_RoomId",
                table: "Depositions",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Depositions_WitnessId",
                table: "Depositions",
                column: "WitnessId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_DepositionId",
                table: "Participants",
                column: "DepositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_UserId",
                table: "Participants",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Users_AddedById",
                table: "Cases",
                column: "AddedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Depositions_DepositionDocuments_CaptionId",
                table: "Depositions",
                column: "CaptionId",
                principalTable: "DepositionDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Depositions_Participants_WitnessId",
                table: "Depositions",
                column: "WitnessId",
                principalTable: "Participants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Users_AddedById",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_DepositionDocuments_Depositions_DepositionId",
                table: "DepositionDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_Participants_Depositions_DepositionId",
                table: "Participants");

            migrationBuilder.DropTable(
                name: "Depositions");

            migrationBuilder.DropTable(
                name: "DepositionDocuments");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.AlterColumn<string>(
                name: "AddedById",
                table: "Cases",
                type: "char(36)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "char(36)");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Users_AddedById",
                table: "Cases",
                column: "AddedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
