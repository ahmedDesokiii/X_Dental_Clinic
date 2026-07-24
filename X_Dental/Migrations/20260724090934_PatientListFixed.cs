using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental.Migrations
{
    /// <inheritdoc />
    public partial class PatientListFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Visits",
                newName: "CreatedDate");

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Visits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Visits",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedBy",
                table: "Visits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "Visits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "TreatmentCatalog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "TreatmentCatalog",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TreatmentCatalog",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedBy",
                table: "TreatmentCatalog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "TreatmentCatalog",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "PatientXRay",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "PatientXRay",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PatientXRay",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedBy",
                table: "PatientXRay",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "PatientXRay",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ERP_Patients_NationalId",
                table: "ERP_Patients",
                column: "NationalId",
                unique: true,
                filter: "[NationalId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ERP_Patients_NationalId",
                table: "ERP_Patients");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TreatmentCatalog");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "TreatmentCatalog");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TreatmentCatalog");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "TreatmentCatalog");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "TreatmentCatalog");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PatientXRay");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "PatientXRay");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PatientXRay");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "PatientXRay");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "PatientXRay");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Visits",
                newName: "CreatedAt");
        }
    }
}
