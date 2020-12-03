using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Metadata;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class PermissionTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Users",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "char(36)", nullable: false),
                    Action = table.Column<string>(nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.Action });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserResourceRoles",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "char(36)", nullable: false),
                    UserId = table.Column<string>(type: "char(36)", nullable: false),
                    ResourceId = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ResourceType = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserResourceRoles", x => new { x.RoleId, x.ResourceId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UserResourceRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserResourceRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[] { "c7f87850-e176-4865-b26b-cedac420a0c8", "CaseAdmin" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "c7f87850-e176-4865-b26b-cedac420a0c8", "Delete" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "c7f87850-e176-4865-b26b-cedac420a0c8", "Update" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "c7f87850-e176-4865-b26b-cedac420a0c8", "View" });

            migrationBuilder.CreateIndex(
                name: "IX_UserResourceRoles_UserId",
                table: "UserResourceRoles",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserResourceRoles");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Users");
        }
    }
}
