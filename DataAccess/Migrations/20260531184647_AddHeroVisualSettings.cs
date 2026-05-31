using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddHeroVisualSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeroMediaType",
                table: "HomePageContents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroMediaUrl",
                table: "HomePageContents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowBadgeCard",
                table: "HomePageContents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowProgressCard",
                table: "HomePageContents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowTeacherCard",
                table: "HomePageContents",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeroMediaType",
                table: "HomePageContents");

            migrationBuilder.DropColumn(
                name: "HeroMediaUrl",
                table: "HomePageContents");

            migrationBuilder.DropColumn(
                name: "ShowBadgeCard",
                table: "HomePageContents");

            migrationBuilder.DropColumn(
                name: "ShowProgressCard",
                table: "HomePageContents");

            migrationBuilder.DropColumn(
                name: "ShowTeacherCard",
                table: "HomePageContents");
        }
    }
}
