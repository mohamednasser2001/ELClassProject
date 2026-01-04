using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class editInstructorForignKeyToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1️⃣ Drop Foreign Keys
            migrationBuilder.DropForeignKey(
                name: "FK_InstructorCourses_Instructors_InstructorId1",
                table: "InstructorCourses");

            migrationBuilder.DropForeignKey(
                name: "FK_InstructorStudents_Instructors_InstructorId1",
                table: "InstructorStudents");

            migrationBuilder.DropForeignKey(
                name: "FK_InstructorStudents_Students_StudentId1",
                table: "InstructorStudents");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCourses_Students_StudentId1",
                table: "StudentCourses");

            // 2️⃣ Drop Indexes
            migrationBuilder.DropIndex(
                name: "IX_StudentCourses_StudentId1",
                table: "StudentCourses");

            migrationBuilder.DropIndex(
                name: "IX_InstructorStudents_InstructorId1",
                table: "InstructorStudents");

            migrationBuilder.DropIndex(
                name: "IX_InstructorStudents_StudentId1",
                table: "InstructorStudents");

            migrationBuilder.DropIndex(
                name: "IX_InstructorCourses_InstructorId1",
                table: "InstructorCourses");

            // 3️⃣ Drop Primary Keys (المهم 🔥)
            migrationBuilder.DropPrimaryKey(
                name: "PK_StudentCourses",
                table: "StudentCourses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InstructorStudents",
                table: "InstructorStudents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InstructorCourses",
                table: "InstructorCourses");

            // 4️⃣ Drop Shadow Columns
            migrationBuilder.DropColumn(
                name: "StudentId1",
                table: "StudentCourses");

            migrationBuilder.DropColumn(
                name: "InstructorId1",
                table: "InstructorStudents");

            migrationBuilder.DropColumn(
                name: "StudentId1",
                table: "InstructorStudents");

            migrationBuilder.DropColumn(
                name: "InstructorId1",
                table: "InstructorCourses");

            // 5️⃣ Alter Columns int → string
            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "StudentCourses",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "InstructorStudents",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<string>(
                name: "InstructorId",
                table: "InstructorStudents",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<string>(
                name: "InstructorId",
                table: "InstructorCourses",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int));

            // 6️⃣ Add Primary Keys تاني
            migrationBuilder.AddPrimaryKey(
                name: "PK_StudentCourses",
                table: "StudentCourses",
                columns: new[] { "StudentId", "CourseId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_InstructorStudents",
                table: "InstructorStudents",
                columns: new[] { "InstructorId", "StudentId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_InstructorCourses",
                table: "InstructorCourses",
                columns: new[] { "InstructorId", "CourseId" });

            // 7️⃣ Index
            migrationBuilder.CreateIndex(
                name: "IX_InstructorStudents_StudentId",
                table: "InstructorStudents",
                column: "StudentId");

            // 8️⃣ Add Foreign Keys
            migrationBuilder.AddForeignKey(
                name: "FK_InstructorCourses_Instructors_InstructorId",
                table: "InstructorCourses",
                column: "InstructorId",
                principalTable: "Instructors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InstructorStudents_Instructors_InstructorId",
                table: "InstructorStudents",
                column: "InstructorId",
                principalTable: "Instructors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InstructorStudents_Students_StudentId",
                table: "InstructorStudents",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCourses_Students_StudentId",
                table: "StudentCourses",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop Foreign Keys
            migrationBuilder.DropForeignKey("FK_InstructorCourses_Instructors_InstructorId", "InstructorCourses");
            migrationBuilder.DropForeignKey("FK_InstructorStudents_Instructors_InstructorId", "InstructorStudents");
            migrationBuilder.DropForeignKey("FK_InstructorStudents_Students_StudentId", "InstructorStudents");
            migrationBuilder.DropForeignKey("FK_StudentCourses_Students_StudentId", "StudentCourses");

            // Drop Index
            migrationBuilder.DropIndex(
                name: "IX_InstructorStudents_StudentId",
                table: "InstructorStudents");

            // Drop PKs
            migrationBuilder.DropPrimaryKey("PK_StudentCourses", "StudentCourses");
            migrationBuilder.DropPrimaryKey("PK_InstructorStudents", "InstructorStudents");
            migrationBuilder.DropPrimaryKey("PK_InstructorCourses", "InstructorCourses");

            // Revert columns
            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "StudentCourses",
                type: "int",
                nullable: false,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "InstructorStudents",
                type: "int",
                nullable: false,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<int>(
                name: "InstructorId",
                table: "InstructorStudents",
                type: "int",
                nullable: false,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<int>(
                name: "InstructorId",
                table: "InstructorCourses",
                type: "int",
                nullable: false,
                oldClrType: typeof(string));

            // Add shadow columns back
            migrationBuilder.AddColumn<string>(
                name: "StudentId1",
                table: "StudentCourses",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstructorId1",
                table: "InstructorStudents",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentId1",
                table: "InstructorStudents",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstructorId1",
                table: "InstructorCourses",
                type: "nvarchar(450)",
                nullable: true);

            // PKs back
            migrationBuilder.AddPrimaryKey(
                name: "PK_StudentCourses",
                table: "StudentCourses",
                columns: new[] { "StudentId", "CourseId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_InstructorStudents",
                table: "InstructorStudents",
                columns: new[] { "InstructorId", "StudentId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_InstructorCourses",
                table: "InstructorCourses",
                columns: new[] { "InstructorId", "CourseId" });
        }

    }
}
