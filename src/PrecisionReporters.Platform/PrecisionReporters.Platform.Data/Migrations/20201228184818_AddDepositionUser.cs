using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddDepositionUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddedById",
                table: "Depositions",
                type: "char(36)",
                nullable: false,
                defaultValue: "");

            // Custom SQL for inserting AddedById before creating FK
            migrationBuilder.Sql("UPDATE Depositions d SET d.AddedById = d.RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Depositions_AddedById",
                table: "Depositions",
                column: "AddedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Depositions_Users_AddedById",
                table: "Depositions",
                column: "AddedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Depositions_Users_AddedById",
                table: "Depositions");

            migrationBuilder.DropIndex(
                name: "IX_Depositions_AddedById",
                table: "Depositions");

            migrationBuilder.DropColumn(
                name: "AddedById",
                table: "Depositions");
        }
    }
}
