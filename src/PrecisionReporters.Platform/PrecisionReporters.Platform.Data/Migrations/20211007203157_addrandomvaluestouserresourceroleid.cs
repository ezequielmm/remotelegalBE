using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class addrandomvaluestouserresourceroleid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE UserResourceRoles set Id = (SELECT UUID()) WHERE Id = '' OR Id = null;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
