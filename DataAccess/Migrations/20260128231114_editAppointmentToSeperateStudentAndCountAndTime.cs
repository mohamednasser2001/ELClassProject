using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class editAppointmentToSeperateStudentAndCountAndTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "Appointments");

            migrationBuilder.AddColumn<DateTime>(
                name: "StudentExpiryDate",
                table: "StudentAppointments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StudentExpiryDate",
                table: "StudentAppointments");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "Appointments",
                type: "datetime2",
                nullable: true);
        }
    }
}
