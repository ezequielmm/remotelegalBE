using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class Add_SystemSettings_Table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.Sql("INSERT INTO SystemSettings(Id,CreationDate,Name,Value)VALUES('914e50de-09b9-40d2-bf9e-aa828946e183',CURRENT_TIMESTAMP,'EnableLiveTranscriptions','enabled')");
            migrationBuilder.Sql("INSERT INTO SystemSettings(Id,CreationDate,Name,Value)VALUES('dd502081-525a-4faa-a9aa-3de98e4368e7',CURRENT_TIMESTAMP,'EnableBreakrooms','enabled');");
            migrationBuilder.Sql("INSERT INTO SystemSettings(Id,CreationDate,Name,Value)VALUES('edbc658a-1d82-4f2f-9e6e-697d0ab0cfe1',CURRENT_TIMESTAMP,'EnableRealTimeTab','enabled');");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
