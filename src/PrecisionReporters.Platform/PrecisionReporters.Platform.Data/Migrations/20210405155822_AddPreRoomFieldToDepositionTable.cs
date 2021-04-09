using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddPreRoomFieldToDepositionTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreRoomId",
                table: "Depositions",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Depositions_PreRoomId",
                table: "Depositions",
                column: "PreRoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Depositions_Rooms_PreRoomId",
                table: "Depositions",
                column: "PreRoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Depositions_Rooms_PreRoomId",
                table: "Depositions");

            migrationBuilder.DropIndex(
                name: "IX_Depositions_PreRoomId",
                table: "Depositions");

            migrationBuilder.DropColumn(
                name: "PreRoomId",
                table: "Depositions");
        }
    }
}
