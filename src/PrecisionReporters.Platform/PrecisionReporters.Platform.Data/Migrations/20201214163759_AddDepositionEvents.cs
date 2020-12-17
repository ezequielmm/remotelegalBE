using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Metadata;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddDepositionEvents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepositionEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    EventType = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(nullable: true),
                    Details = table.Column<string>(nullable: true),
                    DepositionId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepositionEvents_Depositions_DepositionId",
                        column: x => x.DepositionId,
                        principalTable: "Depositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepositionEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepositionEvents_DepositionId",
                table: "DepositionEvents",
                column: "DepositionId");

            migrationBuilder.CreateIndex(
                name: "IX_DepositionEvents_UserId",
                table: "DepositionEvents",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepositionEvents");
        }
    }
}
