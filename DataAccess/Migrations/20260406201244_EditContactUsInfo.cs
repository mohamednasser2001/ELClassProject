using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class EditContactUsInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Message",
                table: "ContactUs");

            migrationBuilder.RenameColumn(
                name: "Topic",
                table: "ContactUs",
                newName: "Course");

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "ContactUs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "ContactUs");

            migrationBuilder.RenameColumn(
                name: "Course",
                table: "ContactUs",
                newName: "Topic");

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "ContactUs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
