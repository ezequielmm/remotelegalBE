using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Metadata;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddAnnotationEvent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnnotationEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AuthorId = table.Column<string>(type: "char(36)", nullable: false),
                    Action = table.Column<string>(nullable: false),
                    DocumentId = table.Column<string>(type: "char(36)", nullable: false),
                    Details = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnotationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnotationEvents_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnnotationEvents_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnnotationEvents_AuthorId",
                table: "AnnotationEvents",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnotationEvents_DocumentId",
                table: "AnnotationEvents",
                column: "DocumentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnnotationEvents");
        }
    }
}
