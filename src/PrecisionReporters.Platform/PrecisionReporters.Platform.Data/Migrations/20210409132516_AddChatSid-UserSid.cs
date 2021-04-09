using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddChatSidUserSid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SId",
                table: "Users",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChatSid",
                table: "Depositions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ChatSid",
                table: "Depositions");
        }
    }
}
