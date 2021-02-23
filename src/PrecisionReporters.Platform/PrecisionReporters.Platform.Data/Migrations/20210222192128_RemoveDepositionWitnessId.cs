using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class RemoveDepositionWitnessId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Participants p SET DepositionId = (SELECT d.Id FROM Depositions d WHERE d.WitnessId = p.Id) WHERE p.Role = 'Witness';");

            migrationBuilder.Sql("ALTER TABLE Depositions DROP FOREIGN KEY FK_Depositions_Participants_WitnessId;");

            migrationBuilder.DropIndex(
                name: "IX_Depositions_WitnessId",
                table: "Depositions");

            migrationBuilder.DropColumn(
                name: "WitnessId",
                table: "Depositions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WitnessId",
                table: "Depositions",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Depositions_WitnessId",
                table: "Depositions",
                column: "WitnessId");

            migrationBuilder.AddForeignKey(
                name: "FK_Depositions_Participants_WitnessId",
                table: "Depositions",
                column: "WitnessId",
                principalTable: "Participants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
