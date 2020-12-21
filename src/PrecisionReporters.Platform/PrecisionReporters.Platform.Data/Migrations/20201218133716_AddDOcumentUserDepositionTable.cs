using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Metadata;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddDOcumentUserDepositionTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(GetDropForeignKeyStatement("DepositionDocuments", "FK_DepositionDocuments_Users_AddedById"));
            migrationBuilder.Sql(GetDropForeignKeyStatement("DepositionDocuments", "FK_DepositionDocuments_Depositions_DepositionId"));
            migrationBuilder.Sql(GetDropForeignKeyStatement("Depositions", "FK_Depositions_DepositionDocuments_CaptionId"));
            migrationBuilder.Sql(GetDropForeignKeyStatement("Depositions", "FK_Depositions_Cases_CaseId"));

            migrationBuilder.DropIndex(
                name: "IX_DepositionDocuments_AddedById",
                table: "DepositionDocuments");

            migrationBuilder.DropColumn(
                name: "AddedById",
                table: "DepositionDocuments");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "DepositionDocuments");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "DepositionDocuments");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "DepositionDocuments");

            migrationBuilder.AlterColumn<string>(
                name: "CaseId",
                table: "Depositions",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(36)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DepositionId",
                table: "DepositionDocuments",
                type: "char(36)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(36)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentId",
                table: "DepositionDocuments",
                type: "char(36)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    FilePath = table.Column<string>(nullable: true),
                    Size = table.Column<long>(nullable: false),
                    AddedById = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Users_AddedById",
                        column: x => x.AddedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentUserDepositions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "char(36)", nullable: false),
                    DocumentId = table.Column<string>(type: "char(36)", nullable: false),
                    DepositionId = table.Column<string>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentUserDepositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentUserDepositions_Depositions_DepositionId",
                        column: x => x.DepositionId,
                        principalTable: "Depositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentUserDepositions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentUserDepositions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepositionDocuments_DocumentId",
                table: "DepositionDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_AddedById",
                table: "Documents",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentUserDepositions_DepositionId",
                table: "DocumentUserDepositions",
                column: "DepositionId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentUserDepositions_DocumentId",
                table: "DocumentUserDepositions",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentUserDepositions_UserId",
                table: "DocumentUserDepositions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DepositionDocuments_Depositions_DepositionId",
                table: "DepositionDocuments",
                column: "DepositionId",
                principalTable: "Depositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DepositionDocuments_Documents_DocumentId",
                table: "DepositionDocuments",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Depositions_Documents_CaptionId",
                table: "Depositions",
                column: "CaptionId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Depositions_Cases_CaseId",
                table: "Depositions",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        private string GetDropForeignKeyStatement(string tableName, string fkName)
        {
            return @$"set @var:=if((SELECT true FROM information_schema.TABLE_CONSTRAINTS WHERE
                    CONSTRAINT_SCHEMA = DATABASE() AND
                    TABLE_NAME        = '{tableName}' AND
                    CONSTRAINT_NAME   = '{fkName}' AND
                    CONSTRAINT_TYPE   = 'FOREIGN KEY') = true,'ALTER TABLE {tableName}
                    drop foreign key {fkName}','select 1');

                    prepare stmt from @var;
                    execute stmt;
                    deallocate prepare stmt;";
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE DepositionDocuments DROP FOREIGN KEY FK_DepositionDocuments_Depositions_DepositionId;");
            migrationBuilder.Sql("ALTER TABLE Depositions DROP FOREIGN KEY FK_Depositions_DepositionDocuments_CaptionId;");
            migrationBuilder.Sql("ALTER TABLE Depositions DROP FOREIGN KEY FK_Depositions_Cases_CaseId;");
           
            migrationBuilder.DropForeignKey(
                name: "FK_Depositions_Cases_CaseId",
                table: "Depositions");

            migrationBuilder.DropTable(
                name: "DocumentUserDepositions");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_DepositionDocuments_DocumentId",
                table: "DepositionDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "DepositionDocuments");

            migrationBuilder.AlterColumn<string>(
                name: "CaseId",
                table: "Depositions",
                type: "char(36)",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "DepositionId",
                table: "DepositionDocuments",
                type: "char(36)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "char(36)");

            migrationBuilder.AddColumn<string>(
                name: "AddedById",
                table: "DepositionDocuments",
                type: "char(36)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "DepositionDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "DepositionDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "DepositionDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepositionDocuments_AddedById",
                table: "DepositionDocuments",
                column: "AddedById");

            migrationBuilder.AddForeignKey(
                name: "FK_DepositionDocuments_Users_AddedById",
                table: "DepositionDocuments",
                column: "AddedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DepositionDocuments_Depositions_DepositionId",
                table: "DepositionDocuments",
                column: "DepositionId",
                principalTable: "Depositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Depositions_DepositionDocuments_CaptionId",
                table: "Depositions",
                column: "CaptionId",
                principalTable: "DepositionDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Depositions_Cases_CaseId",
                table: "Depositions",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
