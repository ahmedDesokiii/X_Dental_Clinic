using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitEntityAndFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Visits",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Visits");
        }
    }
}
