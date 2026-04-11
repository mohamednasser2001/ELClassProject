using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class EditInstructorBioWithFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BioAr",
                table: "Instructors");

            migrationBuilder.RenameColumn(
                name: "BioEn",
                table: "Instructors",
                newName: "Bio");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Bio",
                table: "Instructors",
                newName: "BioEn");

            migrationBuilder.AddColumn<string>(
                name: "BioAr",
                table: "Instructors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
