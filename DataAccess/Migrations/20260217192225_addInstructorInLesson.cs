using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addInstructorInLesson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstructorId",
                table: "Lesson",
                type: "nvarchar(255)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Lesson_InstructorId",
                table: "Lesson",
                column: "InstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lesson_Instructors_InstructorId",
                table: "Lesson",
                column: "InstructorId",
                principalTable: "Instructors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lesson_Instructors_InstructorId",
                table: "Lesson");

            migrationBuilder.DropIndex(
                name: "IX_Lesson_InstructorId",
                table: "Lesson");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Lesson");
        }
    }
}
