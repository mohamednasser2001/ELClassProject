namespace Models
{
    public class HomePageContent
    {
        public int Id { get; set; }

        /// <summary>"en" or "ar"</summary>
        public string Language { get; set; } = "en";

        /// <summary>null for English; "SA","AE","KW","BH","OM","EG","QA" for Arabic</summary>
        public string? CountryCode { get; set; }

        // ── Top Bar ──────────────────────────────────────────────
        public string? WelcomeText { get; set; }
        public string? PhoneNumber { get; set; }

        // ── Hero / Banner ────────────────────────────────────────
        public string? HeroTitle { get; set; }
        public string? HeroTitleHighlight { get; set; }
        public string? HeroTitleSuffix { get; set; }
        public string? HeroSubtitle { get; set; }
        public string? HeroStartBtnText { get; set; }
        public string? VideoUrl { get; set; }
        public string? PlayVideoText { get; set; }

        // ── Counter ──────────────────────────────────────────────
        public int? Counter1Num { get; set; }
        public string? Counter1Label { get; set; }
        public int? Counter2Num { get; set; }
        public string? Counter2Label { get; set; }
        public int? Counter3Num { get; set; }
        public string? Counter3Label { get; set; }
        public int? Counter4Num { get; set; }
        public string? Counter4Label { get; set; }

        // ── About ─────────────────────────────────────────────────
        public string? AboutTopHeading { get; set; }
        public string? AboutMainHeading { get; set; }
        public string? AboutParagraph { get; set; }
        public string? AboutBullet1 { get; set; }
        public string? AboutBullet2 { get; set; }
        public string? AboutBullet3 { get; set; }
        public string? AboutBullet4 { get; set; }
        public string? AboutReadMoreBtn { get; set; }

        // ── Courses ───────────────────────────────────────────────
        public string? CoursesTopHeading { get; set; }
        public string? CoursesMainHeading { get; set; }
        public string? CoursesParagraph { get; set; }

        // ── Team ──────────────────────────────────────────────────
        public string? TeamTopHeading { get; set; }
        public string? TeamMainHeading { get; set; }
        public string? TeamParagraph { get; set; }

        // ── Enroll / Contact ──────────────────────────────────────
        public string? EnrollTopHeading { get; set; }
        public string? EnrollMainHeading { get; set; }
        public string? EnrollParagraph1 { get; set; }
        public string? EnrollParagraph2 { get; set; }
        public string? EnrollBtnText { get; set; }
        public string? EnrollFormTitle { get; set; }
        public string? EnrollFormSubtitle { get; set; }

        // ── How It Works ──────────────────────────────────────────
        public string? WorkTopHeading { get; set; }
        public string? WorkMainHeading { get; set; }
        public string? WorkParagraph { get; set; }
        public string? Step1Title { get; set; }
        public string? Step1Desc { get; set; }
        public string? Step2Title { get; set; }
        public string? Step2Desc { get; set; }
        public string? Step3Title { get; set; }
        public string? Step3Desc { get; set; }
        public string? Step4Title { get; set; }
        public string? Step4Desc { get; set; }

        // ── Testimonials ──────────────────────────────────────────
        public string? TestimonialsTopHeading { get; set; }
        public string? TestimonialsMainHeading { get; set; }
        public string? TestimonialsParagraph { get; set; }
    }
}
