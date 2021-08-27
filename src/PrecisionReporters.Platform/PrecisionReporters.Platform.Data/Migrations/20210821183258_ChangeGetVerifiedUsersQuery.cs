using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class ChangeGetVerifiedUsersQuery : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE VerifyUsers SET VerificationDate = '1971-01-01 00:00:00.000' WHERE VerificationDate IS NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
