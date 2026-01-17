using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedDateInInstructorCourseLesson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_AspNetUsers_CreatedById",
                table: "Courses");

            migrationBuilder.RenameColumn(
                name: "UpdatedAT",
                table: "StudentCourses",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAT",
                table: "InstructorStudents",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAT",
                table: "InstructorCourses",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAT",
                table: "Courses",
                newName: "UpdatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Students",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Lessons",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Lessons",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Lessons",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Instructors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Instructors",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Instructors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CreatedById",
                table: "Lessons",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Instructors_CreatedById",
                table: "Instructors",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_AspNetUsers_CreatedById",
                table: "Courses",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Instructors_AspNetUsers_CreatedById",
                table: "Instructors",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_AspNetUsers_CreatedById",
                table: "Lessons",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_AspNetUsers_CreatedById",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Instructors_AspNetUsers_CreatedById",
                table: "Instructors");

            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_AspNetUsers_CreatedById",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_CreatedById",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Instructors_CreatedById",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Instructors");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "StudentCourses",
                newName: "UpdatedAT");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "InstructorStudents",
                newName: "UpdatedAT");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "InstructorCourses",
                newName: "UpdatedAT");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Courses",
                newName: "UpdatedAT");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_AspNetUsers_CreatedById",
                table: "Courses",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
