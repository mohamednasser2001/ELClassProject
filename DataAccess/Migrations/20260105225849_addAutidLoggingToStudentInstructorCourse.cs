using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addAutidLoggingToStudentInstructorCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_AspNetUsers_CreateById",
                table: "Courses");

            migrationBuilder.RenameColumn(
                name: "CreateById",
                table: "Courses",
                newName: "CreatedById");

            migrationBuilder.RenameColumn(
                name: "CreateAT",
                table: "Courses",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Courses_CreateById",
                table: "Courses",
                newName: "IX_Courses_CreatedById");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "StudentCourses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "StudentCourses",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAT",
                table: "StudentCourses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "InstructorStudents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "InstructorStudents",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAT",
                table: "InstructorStudents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "InstructorCourses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "InstructorCourses",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAT",
                table: "InstructorCourses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourses_CreatedById",
                table: "StudentCourses",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorStudents_CreatedById",
                table: "InstructorStudents",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorCourses_CreatedById",
                table: "InstructorCourses",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_AspNetUsers_CreatedById",
                table: "Courses",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InstructorCourses_AspNetUsers_CreatedById",
                table: "InstructorCourses",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InstructorStudents_AspNetUsers_CreatedById",
                table: "InstructorStudents",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCourses_AspNetUsers_CreatedById",
                table: "StudentCourses",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_AspNetUsers_CreatedById",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_InstructorCourses_AspNetUsers_CreatedById",
                table: "InstructorCourses");

            migrationBuilder.DropForeignKey(
                name: "FK_InstructorStudents_AspNetUsers_CreatedById",
                table: "InstructorStudents");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCourses_AspNetUsers_CreatedById",
                table: "StudentCourses");

            migrationBuilder.DropIndex(
                name: "IX_StudentCourses_CreatedById",
                table: "StudentCourses");

            migrationBuilder.DropIndex(
                name: "IX_InstructorStudents_CreatedById",
                table: "InstructorStudents");

            migrationBuilder.DropIndex(
                name: "IX_InstructorCourses_CreatedById",
                table: "InstructorCourses");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "StudentCourses");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "StudentCourses");

            migrationBuilder.DropColumn(
                name: "UpdatedAT",
                table: "StudentCourses");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "InstructorStudents");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "InstructorStudents");

            migrationBuilder.DropColumn(
                name: "UpdatedAT",
                table: "InstructorStudents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "InstructorCourses");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "InstructorCourses");

            migrationBuilder.DropColumn(
                name: "UpdatedAT",
                table: "InstructorCourses");

            migrationBuilder.RenameColumn(
                name: "CreatedById",
                table: "Courses",
                newName: "CreateById");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Courses",
                newName: "CreateAT");

            migrationBuilder.RenameIndex(
                name: "IX_Courses_CreatedById",
                table: "Courses",
                newName: "IX_Courses_CreateById");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_AspNetUsers_CreateById",
                table: "Courses",
                column: "CreateById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
