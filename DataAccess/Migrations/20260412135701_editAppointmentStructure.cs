using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class editAppointmentStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Students_StudentId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_StudentId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "StudentExpiryDate",
                table: "StudentAppointments");

            migrationBuilder.DropColumn(
                name: "TimeCount",
                table: "StudentAppointments");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Appointments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StudentExpiryDate",
                table: "StudentAppointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeCount",
                table: "StudentAppointments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "Appointments",
                type: "nvarchar(255)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_StudentId",
                table: "Appointments",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Students_StudentId",
                table: "Appointments",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id");
        }
    }
}
