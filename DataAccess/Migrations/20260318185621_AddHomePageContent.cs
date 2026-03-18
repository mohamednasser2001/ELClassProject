using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddHomePageContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomePageContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WelcomeText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeroTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeroTitleHighlight = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeroTitleSuffix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeroSubtitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeroStartBtnText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlayVideoText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Counter1Num = table.Column<int>(type: "int", nullable: true),
                    Counter1Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Counter2Num = table.Column<int>(type: "int", nullable: true),
                    Counter2Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Counter3Num = table.Column<int>(type: "int", nullable: true),
                    Counter3Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Counter4Num = table.Column<int>(type: "int", nullable: true),
                    Counter4Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AboutTopHeading = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AboutMainHeading = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AboutParagraph = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AboutBullet1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AboutBullet2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AboutBullet3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AboutBullet4 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AboutReadMoreBtn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoursesTopHeading = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoursesMainHeading = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoursesParagraph = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TeamTopHeading = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TeamMainHeading = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TeamParagraph = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnrollTopHeading = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnrollMainHeading = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnrollParagraph1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnrollParagraph2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnrollBtnText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnrollFormTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnrollFormSubtitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkTopHeading = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkMainHeading = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkParagraph = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Step1Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Step1Desc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Step2Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Step2Desc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Step3Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Step3Desc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Step4Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Step4Desc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestimonialsTopHeading = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestimonialsMainHeading = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TestimonialsParagraph = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomePageContents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomePageContents");
        }
    }
}
