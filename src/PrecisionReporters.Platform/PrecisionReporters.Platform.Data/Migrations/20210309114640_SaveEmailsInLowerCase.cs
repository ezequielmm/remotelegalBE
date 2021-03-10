using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class SaveEmailsInLowerCase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Participants SET Email = LOWER(Email);");
            migrationBuilder.Sql("UPDATE Users SET EmailAddress = LOWER(EmailAddress);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
