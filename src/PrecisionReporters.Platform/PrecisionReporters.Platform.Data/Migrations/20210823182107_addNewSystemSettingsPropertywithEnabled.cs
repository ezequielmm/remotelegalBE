using Microsoft.EntityFrameworkCore.Migrations;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class addNewSystemSettingsPropertywithEnabled : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("INSERT INTO SystemSettings(Id,CreationDate,Name,Value)VALUES('8e44479e-8581-49c8-82e4-20cdd47b54d4',CURRENT_TIMESTAMP,'EnableTwilioLogs','enabled')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
