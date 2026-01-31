using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class editAppointmentRelationFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentAppointments_Appointments_AppointmentId1",
                table: "StudentAppointments");

            migrationBuilder.DropIndex(
                name: "IX_StudentAppointments_AppointmentId1",
                table: "StudentAppointments");

            migrationBuilder.DropColumn(
                name: "AppointmentId1",
                table: "StudentAppointments");

            migrationBuilder.AddColumn<DateTime>(
                name: "AttendedAt",
                table: "StudentAppointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAttended",
                table: "StudentAppointments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendedAt",
                table: "StudentAppointments");

            migrationBuilder.DropColumn(
                name: "IsAttended",
                table: "StudentAppointments");

            migrationBuilder.AddColumn<int>(
                name: "AppointmentId1",
                table: "StudentAppointments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAppointments_AppointmentId1",
                table: "StudentAppointments",
                column: "AppointmentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentAppointments_Appointments_AppointmentId1",
                table: "StudentAppointments",
                column: "AppointmentId1",
                principalTable: "Appointments",
                principalColumn: "Id");
        }
    }
}
