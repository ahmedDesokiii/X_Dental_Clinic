using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPsychologist.Data.Migrations
{
    /// <inheritdoc />
    public partial class RegionFeildToStudentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "Gender",
                table: "Students",
                type: "bit",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Students",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Students");

            migrationBuilder.AlterColumn<int>(
                name: "Gender",
                table: "Students",
                type: "int",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");
        }
    }
}
