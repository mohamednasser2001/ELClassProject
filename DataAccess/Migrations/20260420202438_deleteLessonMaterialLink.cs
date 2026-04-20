using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class deleteLessonMaterialLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignmentPdfUrl",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "LecturePdfUrl",
                table: "Lesson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignmentPdfUrl",
                table: "Lesson",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LecturePdfUrl",
                table: "Lesson",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
