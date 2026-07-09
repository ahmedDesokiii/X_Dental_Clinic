using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPsychologist.Data.Migrations
{
    /// <inheritdoc />
    public partial class SocialReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SocialReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    FamilyStatus = table.Column<int>(type: "int", nullable: false),
                    EconomicLevel = table.Column<int>(type: "int", nullable: false),
                    FamilyRelation = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialReports_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SocialReports_StudentId",
                table: "SocialReports",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SocialReports");
        }
    }
}
