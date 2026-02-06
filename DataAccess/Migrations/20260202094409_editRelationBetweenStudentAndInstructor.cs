using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class editRelationBetweenStudentAndInstructor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InstructorStudents_Instructors_InstructorId1",
                table: "InstructorStudents");

            migrationBuilder.DropForeignKey(
                name: "FK_InstructorStudents_Students_StudentId1",
                table: "InstructorStudents");

            migrationBuilder.DropIndex(
                name: "IX_InstructorStudents_InstructorId1",
                table: "InstructorStudents");

            migrationBuilder.DropIndex(
                name: "IX_InstructorStudents_StudentId1",
                table: "InstructorStudents");

            migrationBuilder.DropColumn(
                name: "InstructorId1",
                table: "InstructorStudents");

            migrationBuilder.DropColumn(
                name: "StudentId1",
                table: "InstructorStudents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstructorId1",
                table: "InstructorStudents",
                type: "nvarchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentId1",
                table: "InstructorStudents",
                type: "nvarchar(255)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstructorStudents_InstructorId1",
                table: "InstructorStudents",
                column: "InstructorId1");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorStudents_StudentId1",
                table: "InstructorStudents",
                column: "StudentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_InstructorStudents_Instructors_InstructorId1",
                table: "InstructorStudents",
                column: "InstructorId1",
                principalTable: "Instructors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InstructorStudents_Students_StudentId1",
                table: "InstructorStudents",
                column: "StudentId1",
                principalTable: "Students",
                principalColumn: "Id");
        }
    }
}
