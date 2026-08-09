using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental.Migrations
{
    /// <inheritdoc />
    public partial class ClinicalEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientXRay_ERP_Patients_PatientId",
                table: "PatientXRay");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientXRay_Visits_VisitId",
                table: "PatientXRay");

            migrationBuilder.DropForeignKey(
                name: "FK_Visits_TreatmentCatalog_TreatmentCatalogId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_TreatmentCatalogId",
                table: "Visits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TreatmentCatalog",
                table: "TreatmentCatalog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PatientXRay",
                table: "PatientXRay");

            migrationBuilder.DropColumn(
                name: "RequiredAmount",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "TreatmentCatalogId",
                table: "Visits");

            migrationBuilder.RenameTable(
                name: "TreatmentCatalog",
                newName: "TreatmentCatalogs");

            migrationBuilder.RenameTable(
                name: "PatientXRay",
                newName: "PatientXRays");

            migrationBuilder.RenameColumn(
                name: "TreatmentDetails",
                table: "Visits",
                newName: "Examination");

            migrationBuilder.RenameColumn(
                name: "NextVisitDate",
                table: "Visits",
                newName: "FollowUpDate");

            migrationBuilder.RenameIndex(
                name: "IX_PatientXRay_VisitId",
                table: "PatientXRays",
                newName: "IX_PatientXRays_VisitId");

            migrationBuilder.RenameIndex(
                name: "IX_PatientXRay_PatientId",
                table: "PatientXRays",
                newName: "IX_PatientXRays_PatientId");

            migrationBuilder.AddColumn<string>(
                name: "ChiefComplaint",
                table: "Visits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Visits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "Visits",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FileNo",
                table: "ERP_Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Occupation",
                table: "ERP_Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatientType",
                table: "ERP_Patients",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TreatmentCatalogs",
                table: "TreatmentCatalogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PatientXRays",
                table: "PatientXRays",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    AppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VisitId = table.Column<int>(type: "int", nullable: true),
                    DoctorId = table.Column<int>(type: "int", nullable: true),
                    DoctorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClinicId = table.Column<int>(type: "int", nullable: true),
                    IsReminderSent = table.Column<bool>(type: "bit", nullable: false),
                    ReminderDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_ERP_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "ERP_Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appointments_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DentalChartEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    VisitId = table.Column<int>(type: "int", nullable: true),
                    ToothNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SurfacesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcedureCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DentalChartEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DentalChartEntries_ERP_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "ERP_Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DentalChartEntries_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DiagnosisCatalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosisCatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<int>(type: "int", nullable: false),
                    PrescriptionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Diagnosis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PrescribedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DoctorId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcedureCatalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcedureCatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    PlanCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalEstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentPlans_ERP_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "ERP_Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceiptNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitPayments_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitDiagnoses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<int>(type: "int", nullable: false),
                    DiagnosisCatalogId = table.Column<int>(type: "int", nullable: false),
                    ToothNumber = table.Column<int>(type: "int", nullable: true),
                    Surface = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitDiagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitDiagnoses_DiagnosisCatalogs_DiagnosisCatalogId",
                        column: x => x.DiagnosisCatalogId,
                        principalTable: "DiagnosisCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VisitDiagnoses_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrescriptionId = table.Column<int>(type: "int", nullable: false),
                    MedicationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Frequency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Duration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionItems_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitProcedures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitId = table.Column<int>(type: "int", nullable: false),
                    ProcedureCatalogId = table.Column<int>(type: "int", nullable: false),
                    ToothNumber = table.Column<int>(type: "int", nullable: true),
                    Surface = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitProcedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitProcedures_ProcedureCatalogs_ProcedureCatalogId",
                        column: x => x.ProcedureCatalogId,
                        principalTable: "ProcedureCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VisitProcedures_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentPlanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TreatmentPlanId = table.Column<int>(type: "int", nullable: false),
                    ProcedureCatalogId = table.Column<int>(type: "int", nullable: true),
                    ToothNumber = table.Column<int>(type: "int", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentPlanItems_ProcedureCatalogs_ProcedureCatalogId",
                        column: x => x.ProcedureCatalogId,
                        principalTable: "ProcedureCatalogs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TreatmentPlanItems_TreatmentPlans_TreatmentPlanId",
                        column: x => x.TreatmentPlanId,
                        principalTable: "TreatmentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TreatmentPlanId = table.Column<int>(type: "int", nullable: false),
                    VisitId = table.Column<int>(type: "int", nullable: true),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentSessions_TreatmentPlans_TreatmentPlanId",
                        column: x => x.TreatmentPlanId,
                        principalTable: "TreatmentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TreatmentSessions_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_VisitId",
                table: "Appointments",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_DentalChartEntries_PatientId",
                table: "DentalChartEntries",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DentalChartEntries_VisitId",
                table: "DentalChartEntries",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_PrescriptionId",
                table: "PrescriptionItems",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_VisitId",
                table: "Prescriptions",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlanItems_ProcedureCatalogId",
                table: "TreatmentPlanItems",
                column: "ProcedureCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlanItems_TreatmentPlanId",
                table: "TreatmentPlanItems",
                column: "TreatmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_PatientId",
                table: "TreatmentPlans",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSessions_TreatmentPlanId",
                table: "TreatmentSessions",
                column: "TreatmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSessions_VisitId",
                table: "TreatmentSessions",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitDiagnoses_DiagnosisCatalogId",
                table: "VisitDiagnoses",
                column: "DiagnosisCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitDiagnoses_VisitId",
                table: "VisitDiagnoses",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitPayments_VisitId",
                table: "VisitPayments",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitProcedures_ProcedureCatalogId",
                table: "VisitProcedures",
                column: "ProcedureCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitProcedures_VisitId",
                table: "VisitProcedures",
                column: "VisitId");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientXRays_ERP_Patients_PatientId",
                table: "PatientXRays",
                column: "PatientId",
                principalTable: "ERP_Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientXRays_Visits_VisitId",
                table: "PatientXRays",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientXRays_ERP_Patients_PatientId",
                table: "PatientXRays");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientXRays_Visits_VisitId",
                table: "PatientXRays");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "DentalChartEntries");

            migrationBuilder.DropTable(
                name: "PrescriptionItems");

            migrationBuilder.DropTable(
                name: "TreatmentPlanItems");

            migrationBuilder.DropTable(
                name: "TreatmentSessions");

            migrationBuilder.DropTable(
                name: "VisitDiagnoses");

            migrationBuilder.DropTable(
                name: "VisitPayments");

            migrationBuilder.DropTable(
                name: "VisitProcedures");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "TreatmentPlans");

            migrationBuilder.DropTable(
                name: "DiagnosisCatalogs");

            migrationBuilder.DropTable(
                name: "ProcedureCatalogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TreatmentCatalogs",
                table: "TreatmentCatalogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PatientXRays",
                table: "PatientXRays");

            migrationBuilder.DropColumn(
                name: "ChiefComplaint",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "FileNo",
                table: "ERP_Patients");

            migrationBuilder.DropColumn(
                name: "Occupation",
                table: "ERP_Patients");

            migrationBuilder.DropColumn(
                name: "PatientType",
                table: "ERP_Patients");

            migrationBuilder.RenameTable(
                name: "TreatmentCatalogs",
                newName: "TreatmentCatalog");

            migrationBuilder.RenameTable(
                name: "PatientXRays",
                newName: "PatientXRay");

            migrationBuilder.RenameColumn(
                name: "FollowUpDate",
                table: "Visits",
                newName: "NextVisitDate");

            migrationBuilder.RenameColumn(
                name: "Examination",
                table: "Visits",
                newName: "TreatmentDetails");

            migrationBuilder.RenameIndex(
                name: "IX_PatientXRays_VisitId",
                table: "PatientXRay",
                newName: "IX_PatientXRay_VisitId");

            migrationBuilder.RenameIndex(
                name: "IX_PatientXRays_PatientId",
                table: "PatientXRay",
                newName: "IX_PatientXRay_PatientId");

            migrationBuilder.AddColumn<decimal>(
                name: "RequiredAmount",
                table: "Visits",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TreatmentCatalogId",
                table: "Visits",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TreatmentCatalog",
                table: "TreatmentCatalog",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PatientXRay",
                table: "PatientXRay",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_TreatmentCatalogId",
                table: "Visits",
                column: "TreatmentCatalogId");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientXRay_ERP_Patients_PatientId",
                table: "PatientXRay",
                column: "PatientId",
                principalTable: "ERP_Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientXRay_Visits_VisitId",
                table: "PatientXRay",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Visits_TreatmentCatalog_TreatmentCatalogId",
                table: "Visits",
                column: "TreatmentCatalogId",
                principalTable: "TreatmentCatalog",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
