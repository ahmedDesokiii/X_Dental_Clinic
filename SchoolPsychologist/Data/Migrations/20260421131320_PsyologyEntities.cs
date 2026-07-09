using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPsychologist.Data.Migrations
{
    /// <inheritdoc />
    public partial class PsyologyEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Psy_Cases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FollowUpDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Psy_Cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Psy_Cases_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Psy_Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Psy_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Psy_Reports_Psy_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Psy_Cases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Psy_Reports_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Psy_Cases_StudentId",
                table: "Psy_Cases",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Psy_Reports_CaseId",
                table: "Psy_Reports",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Psy_Reports_StudentId",
                table: "Psy_Reports",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Psy_Reports");

            migrationBuilder.DropTable(
                name: "Psy_Cases");
        }
    }
}
