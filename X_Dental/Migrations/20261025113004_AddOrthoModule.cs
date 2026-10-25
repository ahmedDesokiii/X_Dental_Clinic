using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental.Migrations
{
    /// <inheritdoc />
    public partial class AddOrthoModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrthoCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    ArchType = table.Column<int>(type: "int", nullable: false),
                    TotalRequired = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnchorVisitId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrthoCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrthoProcedureCatalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrthoProcedureCatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrthoTotalChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrthoCaseId = table.Column<int>(type: "int", nullable: false),
                    OldTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrthoTotalChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrthoTotalChanges_OrthoCases_OrthoCaseId",
                        column: x => x.OrthoCaseId,
                        principalTable: "OrthoCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrthoVisitEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrthoCaseId = table.Column<int>(type: "int", nullable: false),
                    VisitId = table.Column<int>(type: "int", nullable: false),
                    Arch = table.Column<int>(type: "int", nullable: false),
                    OrthoProcedureId = table.Column<int>(type: "int", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrthoVisitEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrthoVisitEntries_OrthoCases_OrthoCaseId",
                        column: x => x.OrthoCaseId,
                        principalTable: "OrthoCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrthoTotalChanges_OrthoCaseId",
                table: "OrthoTotalChanges",
                column: "OrthoCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_OrthoVisitEntries_OrthoCaseId",
                table: "OrthoVisitEntries",
                column: "OrthoCaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrthoProcedureCatalogs");

            migrationBuilder.DropTable(
                name: "OrthoTotalChanges");

            migrationBuilder.DropTable(
                name: "OrthoVisitEntries");

            migrationBuilder.DropTable(
                name: "OrthoCases");
        }
    }
}
