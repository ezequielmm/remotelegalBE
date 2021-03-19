using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddIsMutedColumnToParticipantsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMuted",
                table: "Participants",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMuted",
                table: "Participants");
        }
    }
}
