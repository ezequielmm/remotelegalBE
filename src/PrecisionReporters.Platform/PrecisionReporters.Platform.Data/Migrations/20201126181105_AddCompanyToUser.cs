using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddCompanyToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyAddress",
                table: "Users",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Users",
                nullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyAddress",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Users");
        }
    }
}
