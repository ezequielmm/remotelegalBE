using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class StoreGuidsAsChars : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE Cases DROP FOREIGN KEY FK_Cases_Users_AddedById;");
            migrationBuilder.Sql("ALTER TABLE VerifyUsers DROP FOREIGN KEY FK_VerifyUsers_Users_UserId;");
            migrationBuilder.Sql("ALTER TABLE Members DROP FOREIGN KEY FK_Members_Users_UserId;");
            migrationBuilder.Sql("ALTER TABLE Members DROP FOREIGN KEY FK_Members_Cases_CaseId;");

            ChangeColumnToChar(migrationBuilder, "VerifyUsers", "UserId", true);
            ChangeColumnToChar(migrationBuilder, "VerifyUsers", "Id", false);
            CreatePrimaryKey(migrationBuilder, "PK_Cases", "VerifyUsers", "Id");
            ChangeColumnToChar(migrationBuilder, "Users", "Id", false);
            CreatePrimaryKey(migrationBuilder, "PK_Users", "Users", "Id");
            ChangeColumnToChar(migrationBuilder, "Rooms", "Id", false);
            CreatePrimaryKey(migrationBuilder, "PK_Rooms", "Rooms", "Id");
            ChangeColumnToChar(migrationBuilder, "Members", "UserId", false);
            ChangeColumnToChar(migrationBuilder, "Members", "CaseId", false);
            ChangeColumnToChar(migrationBuilder, "Members", "Id", false);
            CreatePrimaryKey(migrationBuilder, "PK_Members", "Members", "Id");
            ChangeColumnToChar(migrationBuilder, "Cases", "AddedById", true);
            ChangeColumnToChar(migrationBuilder, "Cases", "Id", false);
            CreatePrimaryKey(migrationBuilder, "PK_Cases", "Cases", "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Users_AddedById",
                table: "Cases",
                column: "AddedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.RenameIndex("FK_Cases_Users_AddedById", "IX_Cases_AddedById", "Cases");

            migrationBuilder.AddForeignKey(
                name: "FK_VerifyUsers_Users_UserId",
                table: "VerifyUsers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.RenameIndex("FK_VerifyUsers_Users_UserId", "IX_VerifyUsers_UserId", "VerifyUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Users_UserId",
                table: "Members",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.RenameIndex("FK_Members_Users_UserId", "IX_Members_UserId", "Members");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Cases_CaseId",
                table: "Members",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.RenameIndex("FK_Members_Cases_CaseId", "IX_Members_CaseId", "Members");
        }

        private void CreatePrimaryKey(MigrationBuilder migrationBuilder, string keyName, string tableName, string columnName)
        {
            migrationBuilder.Sql($"ALTER TABLE {tableName} ADD CONSTRAINT {keyName} PRIMARY KEY ({columnName});");
        }

        private void ChangeColumnToChar(MigrationBuilder migrationBuilder, string tableName, string columnName, bool nullable)
        {
            migrationBuilder.AddColumn<string>(
                            name: $"{columnName}Temp",
                            table: tableName,
                            nullable: nullable,
                            type: "char(36)");

            migrationBuilder.Sql($"UPDATE {tableName} SET {columnName}Temp = " +
                $"LOWER(CONCAT(LEFT(HEX({columnName}), 8), '-', MID(HEX({columnName}), 9,4), '-', MID(HEX({columnName}), 13,4), '-', MID(HEX({columnName}), 17,4), '-', RIGHT(HEX({columnName}), 12)));");

            migrationBuilder.DropColumn(columnName, tableName);

            migrationBuilder.Sql($"ALTER TABLE {tableName} CHANGE `{columnName}Temp` `{columnName}` char(36);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Users_AddedById",
                table: "Cases");

            migrationBuilder.AlterColumn<byte[]>(
                name: "UserId",
                table: "VerifyUsers",
                type: "varbinary(16)",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Id",
                table: "VerifyUsers",
                type: "varbinary(16)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(36)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Id",
                table: "Users",
                type: "varbinary(16)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(36)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Id",
                table: "Rooms",
                type: "varbinary(16)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(36)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "UserId",
                table: "Members",
                type: "varbinary(16)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(36)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "CaseId",
                table: "Members",
                type: "varbinary(16)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(36)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Id",
                table: "Members",
                type: "varbinary(16)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(36)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "AddedById",
                table: "Cases",
                type: "varbinary(16)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "char(36)");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Id",
                table: "Cases",
                type: "varbinary(16)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(36)");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Users_AddedById",
                table: "Cases",
                column: "AddedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
