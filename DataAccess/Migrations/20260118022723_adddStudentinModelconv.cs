using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class adddStudentinModelconv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Conversations_InstructorId",
                table: "Conversations",
                column: "InstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Instructors_InstructorId",
                table: "Conversations",
                column: "InstructorId",
                principalTable: "Instructors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Students_StudentId",
                table: "Conversations",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Instructors_InstructorId",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Students_StudentId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_InstructorId",
                table: "Conversations");
        }
    }
}
