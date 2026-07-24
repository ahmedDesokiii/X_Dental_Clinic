using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstNameAr",
                table: "ERP_Patients");

            migrationBuilder.DropColumn(
                name: "FirstNameEn",
                table: "ERP_Patients");

            migrationBuilder.DropColumn(
                name: "LastNameEn",
                table: "ERP_Patients");

            migrationBuilder.DropColumn(
                name: "MiddleNameAr",
                table: "ERP_Patients");

            migrationBuilder.RenameColumn(
                name: "MiddleNameEn",
                table: "ERP_Patients",
                newName: "FullNameEn");

            migrationBuilder.RenameColumn(
                name: "LastNameAr",
                table: "ERP_Patients",
                newName: "FullNameAr");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DateOfBirth",
                table: "ERP_Patients",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FullNameEn",
                table: "ERP_Patients",
                newName: "MiddleNameEn");

            migrationBuilder.RenameColumn(
                name: "FullNameAr",
                table: "ERP_Patients",
                newName: "LastNameAr");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "ERP_Patients",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstNameAr",
                table: "ERP_Patients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstNameEn",
                table: "ERP_Patients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastNameEn",
                table: "ERP_Patients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MiddleNameAr",
                table: "ERP_Patients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
