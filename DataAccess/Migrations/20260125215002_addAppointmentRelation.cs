using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addAppointmentRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Courses");

            migrationBuilder.AddColumn<string>(
                name: "InstructorId",
                table: "Appointments",
                type: "nvarchar(255)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "StudentAppointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    TimeCount = table.Column<int>(type: "int", nullable: false),
                    AppointmentId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAppointments_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentAppointments_Appointments_AppointmentId1",
                        column: x => x.AppointmentId1,
                        principalTable: "Appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentAppointments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_InstructorId",
                table: "Appointments",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAppointments_AppointmentId",
                table: "StudentAppointments",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAppointments_AppointmentId1",
                table: "StudentAppointments",
                column: "AppointmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAppointments_StudentId",
                table: "StudentAppointments",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Instructors_InstructorId",
                table: "Appointments",
                column: "InstructorId",
                principalTable: "Instructors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Instructors_InstructorId",
                table: "Appointments");

            migrationBuilder.DropTable(
                name: "StudentAppointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_InstructorId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Appointments");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
