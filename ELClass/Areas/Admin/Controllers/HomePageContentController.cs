using DataAccess.Repositories.IRepositories;
using ELClass.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace ELClass.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    [Area("Admin")]
    public class HomePageContentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public static readonly (string Code, string NameAr, string NameEn)[] Countries =
        {
            ("SA", "المملكة العربية السعودية", "Saudi Arabia"),
            ("AE", "الإمارات العربية المتحدة", "United Arab Emirates"),
            ("KW", "الكويت", "Kuwait"),
            ("BH", "البحرين", "Bahrain"),
            ("OM", "عُمان", "Oman"),
            ("EG", "مصر", "Egypt"),
            ("QA", "قطر", "Qatar"),
        };

        public HomePageContentController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            await EnsureAllRecordsExistAsync();
            var allContent = (await _unitOfWork.HomePageContentRepository.GetAsync()).ToList();
            return View(allContent);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(HomePageContent model, string tabId, IFormFile PlayVideoImg)
        {
            var isArabic = CultureHelper.IsArabic;

            if (PlayVideoImg != null && PlayVideoImg.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(PlayVideoImg.FileName)}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "thumbnails", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await PlayVideoImg.CopyToAsync(stream);
                }
                model.PlayVideoText = $"{fileName}";
                var allRecords = await _unitOfWork.HomePageContentRepository.GetAsync();
                foreach (var record in allRecords)
                {
                    record.PlayVideoText = fileName;
                    await _unitOfWork.HomePageContentRepository.EditAsync(record);
                }
            }

            await _unitOfWork.HomePageContentRepository.EditAsync(model);
            TempData["Success"] = isArabic ? "تم حفظ المحتوى بنجاح" : "Content saved successfully";
            return RedirectToAction("Index", new { activeTab = tabId });
        }

        private async Task EnsureAllRecordsExistAsync()
        {
            var all = (await _unitOfWork.HomePageContentRepository.GetAsync()).ToList();

            if (!all.Any(x => x.Language == "en"))
            {
                await _unitOfWork.HomePageContentRepository.CreateAsync(BuildDefaults("en", null));
            }
            else
            {
                var rec = all.First(x => x.Language == "en");
                if (PatchDefaults(rec, "en")) await _unitOfWork.HomePageContentRepository.EditAsync(rec);
            }

            foreach (var (code, _, _) in Countries)
            {
                if (!all.Any(x => x.Language == "ar" && x.CountryCode == code))
                {
                    await _unitOfWork.HomePageContentRepository.CreateAsync(BuildDefaults("ar", code));
                }
                else
                {
                    var rec = all.First(x => x.Language == "ar" && x.CountryCode == code);
                    if (PatchDefaults(rec, "ar")) await _unitOfWork.HomePageContentRepository.EditAsync(rec);
                }
            }
        }

        /// <summary>Fill null fields with defaults. Returns true if anything changed.</summary>
        private static bool PatchDefaults(HomePageContent r, string lang)
        {
            bool ar = lang == "ar";
            bool changed = false;

            string S(string en, string ara) => ar ? ara : en;

            if (r.WelcomeText == null) { r.WelcomeText = S("Welcome To ElClass", "مرحباً بك في الكلاس"); changed = true; }
            if (r.PhoneNumber == null) { r.PhoneNumber = "+(321) 321654897"; changed = true; }

            if (r.HeroTitle == null) { r.HeroTitle = S("Get Started With", "ابحث عن المدرس المثالي لطفلك"); changed = true; }
            if (r.HeroTitleHighlight == null) { r.HeroTitleHighlight = S("ElClass", "الكلاس"); changed = true; }
            if (r.HeroTitleSuffix == null) { r.HeroTitleSuffix = S(" Online | Offline Courses", ""); changed = true; }
            if (r.HeroSubtitle == null)
            {
                r.HeroSubtitle = S(
                    "We help students reach their life goals faster and more efficiently. Growing online learning.",
                    "نساعد الطلاب على تحقيق أهدافهم التعليمية بشكل أسرع وأكثر كفاءة. تعلم عبر الإنترنت في أي مكان وأي وقت.");
                changed = true;
            }
            if (r.HeroStartBtnText == null) { r.HeroStartBtnText = S("get started", "ابدأ الآن"); changed = true; }

            if (r.Counter1Num == null) { r.Counter1Num = 22000; changed = true; }
            if (r.Counter1Label == null) { r.Counter1Label = S("Success Stories", "قصص نجاح"); changed = true; }
            if (r.Counter2Num == null) { r.Counter2Num = 6500; changed = true; }
            if (r.Counter2Label == null) { r.Counter2Label = S("Expert Instructor", "مدرس خبير"); changed = true; }
            if (r.Counter3Num == null) { r.Counter3Num = 5000; changed = true; }
            if (r.Counter3Label == null) { r.Counter3Label = S("Active Student", "طالب نشط"); changed = true; }
            if (r.Counter4Num == null) { r.Counter4Num = 2000; changed = true; }
            if (r.Counter4Label == null) { r.Counter4Label = S("Hours Video Classes", "ساعة فيديو"); changed = true; }

            if (r.AboutTopHeading == null) { r.AboutTopHeading = S("about us", "من نحن"); changed = true; }
            if (r.AboutMainHeading == null) { r.AboutMainHeading = S("Various Types Of Courses Will Give You The Perfect Solution", "أنواع مختلفة من الدورات ستمنحك الحل المثالي"); changed = true; }
            if (r.AboutParagraph == null)
            {
                r.AboutParagraph = S(
                    "Get latest news in your inbox. Consectetur adipiscing elitadipiscing elitseddo eiusmod tempor incididunt ut labore et dolore.",
                    "نوفر لك أحدث الدورات التعليمية. نحن نقدم مجموعة واسعة من المواد الدراسية مع مدرسين ذوي خبرة عالية.");
                changed = true;
            }
            if (r.AboutBullet1 == null) { r.AboutBullet1 = S("1-on-1 private tutoring", "دروس خصوصية فردية (واحد لواحد)"); changed = true; }
            if (r.AboutBullet2 == null) { r.AboutBullet2 = S("Online or at-home sessions", "حصص أونلاين أو في المنزل"); changed = true; }
            if (r.AboutBullet3 == null) { r.AboutBullet3 = S("Track your child's progress", "تتبع تقدم طفلك الدراسي"); changed = true; }
            if (r.AboutBullet4 == null) { r.AboutBullet4 = S("Tutors in all subjects", "مدرسون في جميع المواد"); changed = true; }
            if (r.AboutReadMoreBtn == null) { r.AboutReadMoreBtn = S("read more", "اقرأ المزيد"); changed = true; }

            if (r.CoursesTopHeading == null) { r.CoursesTopHeading = S("COURSES Categories", "فئات الدورات"); changed = true; }
            if (r.CoursesMainHeading == null) { r.CoursesMainHeading = S("We Bring The Good Education To Life", "احصل على دعم تعليمي في جميع المواد"); changed = true; }
            if (r.CoursesParagraph == null)
            {
                r.CoursesParagraph = S(
                    "Consectetur adipiscing elitadipiscing elitseddo eiusmod tempor incididunt ut labore et dolore.",
                    "مجموعة متنوعة من المواد الدراسية من الروضة حتى الجامعة");
                changed = true;
            }

            if (r.TeamTopHeading == null) { r.TeamTopHeading = S("Our instructors", "مدرسونا"); changed = true; }
            if (r.TeamMainHeading == null) { r.TeamMainHeading = S("Classes Taught By Real Creators", "تعرف على مدرسينا المتميزين"); changed = true; }
            if (r.TeamParagraph == null)
            {
                r.TeamParagraph = S(
                    "Consectetur adipiscing elitadipiscing elitseddo eiusmod tempor incididunt ut labore et dolore.",
                    "فريق من المدرسين المؤهلين وذوي الخبرة في جميع المواد الدراسية");
                changed = true;
            }

            if (r.EnrollTopHeading == null) { r.EnrollTopHeading = S("Enroll Now", "سجل الآن"); changed = true; }
            if (r.EnrollMainHeading == null) { r.EnrollMainHeading = S("Start Learning Today!", "ابدأ التعلم اليوم!"); changed = true; }
            if (r.EnrollParagraph1 == null) { r.EnrollParagraph1 = S("Join thousands of students who are already learning with us.", "انضم إلى آلاف الطلاب الذين يتعلمون معنا بالفعل."); changed = true; }
            if (r.EnrollParagraph2 == null) { r.EnrollParagraph2 = S("Find the perfect instructor and start your educational journey.", "ابحث عن المدرس المثالي وابدأ رحلتك التعليمية."); changed = true; }
            if (r.EnrollBtnText == null) { r.EnrollBtnText = S("enroll now", "سجل الآن"); changed = true; }
            if (r.EnrollFormTitle == null) { r.EnrollFormTitle = S("enroll now", "سجل الآن"); changed = true; }
            if (r.EnrollFormSubtitle == null) { r.EnrollFormSubtitle = S("Get details in your inbox...", "احصل على التفاصيل..."); changed = true; }

            if (r.WorkTopHeading == null) { r.WorkTopHeading = S("JOIN US", "انضم إلينا"); changed = true; }
            if (r.WorkMainHeading == null) { r.WorkMainHeading = S("How It Work? Get Your Certificate Now!", "كيف يعمل؟ احصل على شهادتك الآن!"); changed = true; }
            if (r.WorkParagraph == null) { r.WorkParagraph = S("Follow these simple steps to start your learning journey.", "اتبع هذه الخطوات البسيطة لبدء رحلتك التعليمية."); changed = true; }
            if (r.Step1Title == null) { r.Step1Title = S("Create Account", "أنشئ حسابك"); changed = true; }
            if (r.Step1Desc == null) { r.Step1Desc = S("Sign up and create your free account in minutes.", "سجل وأنشئ حسابك المجاني في دقائق."); changed = true; }
            if (r.Step2Title == null) { r.Step2Title = S("Find Instructor", "ابحث عن مدرس"); changed = true; }
            if (r.Step2Desc == null) { r.Step2Desc = S("Browse our certified instructors and pick the right one.", "تصفح مدرسينا المعتمدين واختر المناسب."); changed = true; }
            if (r.Step3Title == null) { r.Step3Title = S("Start Learning", "ابدأ التعلم"); changed = true; }
            if (r.Step3Desc == null) { r.Step3Desc = S("Schedule sessions and start learning at your own pace.", "احجز جلسات وابدأ التعلم بوتيرتك الخاصة."); changed = true; }
            if (r.Step4Title == null) { r.Step4Title = S("Get Certificate", "احصل على الشهادة"); changed = true; }
            if (r.Step4Desc == null) { r.Step4Desc = S("Complete your course and receive your certificate.", "أتمم دورتك واحصل على شهادتك."); changed = true; }

            if (r.TestimonialsTopHeading == null) { r.TestimonialsTopHeading = S("Testimonials", "آراء العملاء"); changed = true; }
            if (r.TestimonialsMainHeading == null) { r.TestimonialsMainHeading = S("What Our Students Say About Us", "ماذا يقول طلابنا عنا"); changed = true; }
            if (r.TestimonialsParagraph == null)
            {
                r.TestimonialsParagraph = S(
                    "Read experiences from our students and parents.",
                    "اقرأ تجارب طلابنا وأولياء الأمور مع منصتنا التعليمية");
                changed = true;
            }

            if (r.PricingTopHeading == null) { r.PricingTopHeading = S("Pricing Plans", "خطط الأسعار"); changed = true; }
            if (r.PricingMainHeading == null) { r.PricingMainHeading = S("Transparent And Simple Pricing", "أسعار شفافة وبسيطة"); changed = true; }
            if (r.PricingParagraph == null) { r.PricingParagraph = S("Choose the plan that works best for you.", "اختر الخطة المناسبة لك وابدأ رحلتك التعليمية اليوم"); changed = true; }
            if (r.PricingMonthlyLabel == null) { r.PricingMonthlyLabel = S("Monthly", "شهري"); changed = true; }
            if (r.PricingYearlyLabel == null) { r.PricingYearlyLabel = S("Yearly", "سنوي"); changed = true; }
            if (r.PricingBtnText == null) { r.PricingBtnText = S("Get Started Now", "ابدأ الآن"); changed = true; }

            if (r.BlogTopHeading == null) { r.BlogTopHeading = S("Our Blogs", "مدونتنا"); changed = true; }
            if (r.BlogMainHeading == null) { r.BlogMainHeading = S("News, Tips, Blogs & Insights", "أخبار ونصائح ورؤى"); changed = true; }
            if (r.BlogParagraph == null) { r.BlogParagraph = S("Stay updated with the latest educational tips.", "ابق على اطلاع بأحدث الأخبار والنصائح التعليمية"); changed = true; }
            if (r.BlogReadMoreText == null) { r.BlogReadMoreText = S("read more", "اقرأ المزيد"); changed = true; }

            return changed;
        }

        private static HomePageContent BuildDefaults(string lang, string? country)
        {
            var r = new HomePageContent { Language = lang, CountryCode = country };
            PatchDefaults(r, lang);
            return r;
        }
    }
}
