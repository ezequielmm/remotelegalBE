using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddSharingDocumentProperty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SharingDocumentId",
                table: "Depositions",
                nullable: true);

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "6c73879b-cce3-47ea-9b80-12e1c4d1285e", "ViewSharedDocument" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "997d199c-3b9a-4103-a320-130b02890a5b", "ViewSharedDocument" });

            migrationBuilder.CreateIndex(
                name: "IX_Depositions_SharingDocumentId",
                table: "Depositions",
                column: "SharingDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Depositions_Documents_SharingDocumentId",
                table: "Depositions",
                column: "SharingDocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE Depositions DROP FOREIGN KEY FK_Depositions_Documents_SharingDocumentId;");

            migrationBuilder.DropIndex(
                name: "IX_Depositions_SharingDocumentId",
                table: "Depositions");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "6c73879b-cce3-47ea-9b80-12e1c4d1285e", "ViewSharedDocument" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "997d199c-3b9a-4103-a320-130b02890a5b", "ViewSharedDocument" });

            migrationBuilder.DropColumn(
                name: "SharingDocumentId",
                table: "Depositions");
        }
    }
}
