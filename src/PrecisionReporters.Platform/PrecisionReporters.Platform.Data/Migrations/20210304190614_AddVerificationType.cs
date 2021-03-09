using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddVerificationType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerificationType",
                table: "VerifyUsers",
                nullable: false);

            migrationBuilder.Sql("UPDATE VerifyUsers SET VerificationType = 'VerifyUser'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerificationType",
                table: "VerifyUsers");
        }
    }
}
