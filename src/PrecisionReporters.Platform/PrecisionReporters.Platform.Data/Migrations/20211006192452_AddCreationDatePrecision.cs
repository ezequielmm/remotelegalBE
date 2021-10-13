using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.EntityFrameworkCore.Metadata;

namespace PrecisionReporters.Platform.Data.Migrations
{
    public partial class AddCreationDatePrecision : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "VerifyUsers",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Users",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "UserResourceRoles",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "TwilioParticipant",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Transcriptions",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "SystemSettings",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Rooms",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Roles",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Participants",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Members",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "DocumentUserDepositions",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Documents",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "DevicesInfo",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Depositions",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "DepositionEvents",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "DepositionDocuments",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Compositions",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Cases",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "BreakRooms",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "AnnotationEvents",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "ActivityHistories",
                type: "datetime(3)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(3)",
                oldClrType: typeof(DateTime),
                oldType: "datetime")
                .OldAnnotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "VerifyUsers",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Users",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "UserResourceRoles",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "TwilioParticipant",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Transcriptions",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "SystemSettings",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Rooms",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Roles",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Participants",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Members",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "DocumentUserDepositions",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Documents",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "DevicesInfo",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Depositions",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "DepositionEvents",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "DepositionDocuments",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Compositions",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "Cases",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "BreakRooms",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "AnnotationEvents",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreationDate",
                table: "ActivityHistories",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(3)",
                oldDefaultValueSql: "CURRENT_TIMESTAMP(3)")
                .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn);
        }
    }
}
