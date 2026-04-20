using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailRemoveAge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "ContactUs");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ContactUs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "ContactUs");

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "ContactUs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
