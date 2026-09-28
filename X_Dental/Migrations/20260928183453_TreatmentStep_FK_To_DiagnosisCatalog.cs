using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental.Migrations
{
    /// <inheritdoc />
    public partial class TreatmentStep_FK_To_DiagnosisCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlanSteps_ProcedureCatalogs_ProcedureCatalogId",
                table: "TreatmentPlanSteps");

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlanSteps_DiagnosisCatalogs_ProcedureCatalogId",
                table: "TreatmentPlanSteps",
                column: "ProcedureCatalogId",
                principalTable: "DiagnosisCatalogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlanSteps_DiagnosisCatalogs_ProcedureCatalogId",
                table: "TreatmentPlanSteps");

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlanSteps_ProcedureCatalogs_ProcedureCatalogId",
                table: "TreatmentPlanSteps",
                column: "ProcedureCatalogId",
                principalTable: "ProcedureCatalogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
