using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental.Migrations
{
    /// <inheritdoc />
    public partial class Dental_Treatment_Module : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ERP_Teeth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ToothNumber = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Jaw = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Side = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ToothType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsPermanent = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ERP_Teeth", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ERP_ToothStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsFinal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ERP_ToothStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ERP_TreatmentPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    PlanNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PlanDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ERP_TreatmentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ERP_TreatmentPlans_ERP_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "ERP_Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ERP_TreatmentProcedures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DefaultPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DefaultDuration = table.Column<int>(type: "int", nullable: false),
                    RequiresTooth = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ERP_TreatmentProcedures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ERP_PatientTeeth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    ToothId = table.Column<int>(type: "int", nullable: false),
                    ToothStatusId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastTreatmentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsMissing = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ERP_PatientTeeth", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ERP_PatientTeeth_ERP_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "ERP_Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ERP_PatientTeeth_ERP_Teeth_ToothId",
                        column: x => x.ToothId,
                        principalTable: "ERP_Teeth",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ERP_PatientTeeth_ERP_ToothStatuses_ToothStatusId",
                        column: x => x.ToothStatusId,
                        principalTable: "ERP_ToothStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ERP_TreatmentPlanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TreatmentPlanId = table.Column<int>(type: "int", nullable: false),
                    TreatmentProcedureId = table.Column<int>(type: "int", nullable: false),
                    ToothId = table.Column<int>(type: "int", nullable: true),
                    DoctorId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ERP_TreatmentPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ERP_TreatmentPlanItems_ERP_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "ERP_Doctors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ERP_TreatmentPlanItems_ERP_Teeth_ToothId",
                        column: x => x.ToothId,
                        principalTable: "ERP_Teeth",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ERP_TreatmentPlanItems_ERP_TreatmentPlans_TreatmentPlanId",
                        column: x => x.TreatmentPlanId,
                        principalTable: "ERP_TreatmentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ERP_TreatmentPlanItems_ERP_TreatmentProcedures_TreatmentProcedureId",
                        column: x => x.TreatmentProcedureId,
                        principalTable: "ERP_TreatmentProcedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ERP_PatientTeeth_PatientId_ToothId",
                table: "ERP_PatientTeeth",
                columns: new[] { "PatientId", "ToothId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ERP_PatientTeeth_ToothId",
                table: "ERP_PatientTeeth",
                column: "ToothId");

            migrationBuilder.CreateIndex(
                name: "IX_ERP_PatientTeeth_ToothStatusId",
                table: "ERP_PatientTeeth",
                column: "ToothStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ERP_Teeth_ToothNumber",
                table: "ERP_Teeth",
                column: "ToothNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ERP_ToothStatuses_Code",
                table: "ERP_ToothStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ERP_TreatmentPlanItems_DoctorId",
                table: "ERP_TreatmentPlanItems",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_ERP_TreatmentPlanItems_ToothId",
                table: "ERP_TreatmentPlanItems",
                column: "ToothId");

            migrationBuilder.CreateIndex(
                name: "IX_ERP_TreatmentPlanItems_TreatmentPlanId",
                table: "ERP_TreatmentPlanItems",
                column: "TreatmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ERP_TreatmentPlanItems_TreatmentProcedureId",
                table: "ERP_TreatmentPlanItems",
                column: "TreatmentProcedureId");

            migrationBuilder.CreateIndex(
                name: "IX_ERP_TreatmentPlans_PatientId",
                table: "ERP_TreatmentPlans",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ERP_TreatmentPlans_PlanNo",
                table: "ERP_TreatmentPlans",
                column: "PlanNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ERP_TreatmentProcedures_Code",
                table: "ERP_TreatmentProcedures",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ERP_PatientTeeth");

            migrationBuilder.DropTable(
                name: "ERP_TreatmentPlanItems");

            migrationBuilder.DropTable(
                name: "ERP_ToothStatuses");

            migrationBuilder.DropTable(
                name: "ERP_Teeth");

            migrationBuilder.DropTable(
                name: "ERP_TreatmentPlans");

            migrationBuilder.DropTable(
                name: "ERP_TreatmentProcedures");
        }
    }
}
