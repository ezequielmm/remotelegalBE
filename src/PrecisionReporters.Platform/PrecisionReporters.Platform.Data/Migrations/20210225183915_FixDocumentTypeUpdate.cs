using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class FixDocumentTypeUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Documents doc SET DocumentType = 'Exhibit' WHERE DocumentType = '0'");
            migrationBuilder.Sql("UPDATE Documents doc SET DocumentType = 'Caption' WHERE DocumentType = '1'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
