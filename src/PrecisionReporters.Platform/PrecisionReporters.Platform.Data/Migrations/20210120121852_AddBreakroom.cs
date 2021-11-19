using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Metadata;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddBreakroom : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BreakRooms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(36)", nullable: false),
                    CreationDate = table.Column<DateTime>(nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    IsLocked = table.Column<bool>(nullable: false),
                    RoomId = table.Column<string>(type: "char(36)", nullable: false),
                    DepositionId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BreakRooms_Depositions_DepositionId",
                        column: x => x.DepositionId,
                        principalTable: "Depositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BreakRooms_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BreakRoomsAttendees",
                columns: table => new
                {
                    BreakRoomId = table.Column<string>(type: "char(36)", nullable: false),
                    UserId = table.Column<string>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakRoomsAttendees", x => new { x.BreakRoomId, x.UserId });
                    table.ForeignKey(
                        name: "FK_BreakRoomsAttendees_BreakRooms_BreakRoomId",
                        column: x => x.BreakRoomId,
                        principalTable: "BreakRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BreakRoomsAttendees_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "997d199c-3b9a-4103-a320-130b02890a5b", "View" });

            migrationBuilder.CreateIndex(
                name: "IX_BreakRooms_DepositionId",
                table: "BreakRooms",
                column: "DepositionId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakRooms_RoomId",
                table: "BreakRooms",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakRoomsAttendees_UserId",
                table: "BreakRoomsAttendees",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BreakRoomsAttendees");

            migrationBuilder.DropTable(
                name: "BreakRooms");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "997d199c-3b9a-4103-a320-130b02890a5b", "View" });
        }
    }
}
