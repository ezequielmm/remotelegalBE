using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class UpdateCompletedDepoPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[] { "4b665d69-5f6f-433d-bbe2-d16abe03d8d0", "DepositionCompletedAtendee" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RoleId", "Action" },
                values: new object[] { "4b665d69-5f6f-433d-bbe2-d16abe03d8d0", "ViewDetails" });
            // Adding DepositionCompletedAtendee Role to Completed Depositions 
            migrationBuilder.Sql(@"INSERT
	                                    INTO
	                                    UserResourceRoles (RoleId,
	                                    UserId,
	                                    ResourceId,
	                                    CreationDate,
	                                    ResourceType)
                                    SELECT DISTINCT
	                                    '4b665d69-5f6f-433d-bbe2-d16abe03d8d0',
	                                    urr.UserId ,
	                                    urr.ResourceId,
	                                    urr.CreationDate ,
	                                    urr.ResourceType
                                    FROM
	                                    Depositions d
                                    INNER JOIN Participants p on
	                                    d.Id = p.DepositionId
                                    INNER JOIN UserResourceRoles urr on
	                                    d.Id = urr.ResourceId
                                    INNER JOIN Roles r on
	                                    urr.RoleId = r.Id
                                    WHERE
	                                    d.Status = 'Completed'
	                                    AND urr.ResourceType = 'Deposition';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM UserResourceRoles 
                                    WHERE RoleId = '4b665d69-5f6f-433d-bbe2-d16abe03d8d0'");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "RoleId", "Action" },
                keyValues: new object[] { "4b665d69-5f6f-433d-bbe2-d16abe03d8d0", "ViewDetails" });

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: "4b665d69-5f6f-433d-bbe2-d16abe03d8d0");
        }
    }
}
