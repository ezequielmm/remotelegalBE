using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AwsSessionInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmazonAvailability",
                table: "ActivityHistories",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "ContainerID",
                table: "ActivityHistories",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmazonAvailability",
                table: "ActivityHistories");
            migrationBuilder.DropColumn(
                name: "ContainerID",
                table: "ActivityHistories");
        }
    }
}
