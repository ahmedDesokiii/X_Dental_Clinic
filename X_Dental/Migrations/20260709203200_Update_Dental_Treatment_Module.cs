using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental.Migrations
{
    /// <inheritdoc />
    public partial class Update_Dental_Treatment_Module : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ERP_TreatmentSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TreatmentPlanId = table.Column<int>(type: "int", nullable: false),
                    AppointmentId = table.Column<int>(type: "int", nullable: true),
                    DoctorId = table.Column<int>(type: "int", nullable: true),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionNo = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    TreatmentPlanItemId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ERP_TreatmentSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ERP_TreatmentSessions_ERP_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "ERP_Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ERP_TreatmentSessions_ERP_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "ERP_Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ERP_TreatmentSessions_ERP_TreatmentPlanItems_TreatmentPlanItemId",
                        column: x => x.TreatmentPlanItemId,
                        principalTable: "ERP_TreatmentPlanItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ERP_TreatmentSessions_ERP_TreatmentPlans_TreatmentPlanId",
                        column: x => x.TreatmentPlanId,
                        principalTable: "ERP_TreatmentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ERP_TreatmentSessions_AppointmentId",
                table: "ERP_TreatmentSessions",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ERP_TreatmentSessions_DoctorId",
                table: "ERP_TreatmentSessions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_ERP_TreatmentSessions_TreatmentPlanId",
                table: "ERP_TreatmentSessions",
                column: "TreatmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ERP_TreatmentSessions_TreatmentPlanItemId",
                table: "ERP_TreatmentSessions",
                column: "TreatmentPlanItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ERP_TreatmentSessions");
        }
    }
}
