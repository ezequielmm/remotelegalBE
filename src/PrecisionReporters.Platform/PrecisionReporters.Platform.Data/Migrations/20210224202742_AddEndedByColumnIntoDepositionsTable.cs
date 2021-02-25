using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddEndedByColumnIntoDepositionsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EndedById",
                table: "Depositions",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Depositions_EndedById",
                table: "Depositions",
                column: "EndedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Depositions_Users_EndedById",
                table: "Depositions",
                column: "EndedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Depositions_Users_EndedById",
                table: "Depositions");

            migrationBuilder.DropIndex(
                name: "IX_Depositions_EndedById",
                table: "Depositions");

            migrationBuilder.DropColumn(
                name: "EndedById",
                table: "Depositions");
        }
    }
}
