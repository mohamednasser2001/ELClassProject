using DataAccess.Repositories.IRepositories;
using ELClass.Hubs;
using ELClass.services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using System.Threading.Tasks;

namespace ELClass.Controllers
{

    public class HomeController(IUnitOfWork unitOfWork , IHubContext<RealTimeHub> realHub , UserManager<ApplicationUser> userManager ,
        SignInManager<ApplicationUser> signInManager,IEmailSender emailSender) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IHubContext<RealTimeHub> _realHub = realHub;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly IEmailSender _emailSender = emailSender;
        private static readonly string[] ValidCountryCodes = { "SA", "AE", "KW", "BH", "OM", "EG", "QA" };

        public async Task<IActionResult> Index()
        {
            var isArabic = CultureHelper.IsArabic;
            string? selectedCountry = null;
            HomePageContent? content;

            if (isArabic)
            {
                selectedCountry = Request.Cookies["el-country"];
                if (string.IsNullOrEmpty(selectedCountry) || !ValidCountryCodes.Contains(selectedCountry))
                    selectedCountry = "SA";
                content = (await _unitOfWork.HomePageContentRepository.GetAsync(
                    filter: x => x.Language == "ar" && x.CountryCode == selectedCountry,
                    tracked: false)).FirstOrDefault();
            }
            else
            {
                content = (await _unitOfWork.HomePageContentRepository.GetAsync(
                    filter: x => x.Language == "en",
                    tracked: false)).FirstOrDefault();
            }

            var instructors = await _unitOfWork.InstructorRepository.GetAsync(include: e => e.Include(e => e.ApplicationUser));

            // Load dynamic sections by language + country
            string lang = isArabic ? "ar" : "en";
            var testimonials = await _unitOfWork.TestimonialRepository.GetAsync(
                filter: x => x.Language == lang && x.CountryCode == selectedCountry && x.IsActive,
                orderBy: q => q.OrderBy(x => x.SortOrder),
                tracked: false);

            var pricingPlans = await _unitOfWork.PricingPlanRepository.GetAsync(
                filter: x => x.Language == lang && x.CountryCode == selectedCountry && x.IsActive,
                orderBy: q => q.OrderBy(x => x.SortOrder),
                tracked: false);

            var articles = await _unitOfWork.ArticleRepository.GetAsync(
                filter: x => x.Language == lang && x.CountryCode == selectedCountry && x.IsActive,
                orderBy: q => q.OrderBy(x => x.SortOrder),
                tracked: false);

            HomePageIndexVM model = new HomePageIndexVM()
            {
                Instructors = instructors.ToList(),
                Content = content,
                SelectedCountry = selectedCountry,
                Testimonials = testimonials.ToList(),
                PricingPlans = pricingPlans.ToList(),
                Articles = articles.ToList()
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult SetCountry(string country, string returnUrl)
        {
            if (!ValidCountryCodes.Contains(country))
                country = "SA";

            Response.Cookies.Append(
                "el-country",
                country,
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return Redirect("/");
        }

        [HttpGet]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );


            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return Redirect("/");
        }

        [HttpPost]
        public async Task<IActionResult> ContactUs(ContactUs contactUs)
        {
            var isArabic = CultureHelper.IsArabic;
            if (ModelState.IsValid)
            {
                await _unitOfWork.ContactUsRepository.CreateAsync(contactUs);
                await _unitOfWork.CommitAsync();


                var existingUser = await _userManager.FindByEmailAsync(contactUs.Email);
                if (existingUser == null && !string.IsNullOrWhiteSpace(contactUs.Email))
                {
                    var userName = await GenerateUniqueUsernameAsync(contactUs.Email);
                    var newUser = new ApplicationUser
                    {
                        Email = contactUs.Email,
                        UserName = userName,
                        NameEN = contactUs.Name,
                        NameAR = contactUs.Name,
                        PhoneNumber = contactUs.PhoneNumber,
                    };
                    var result = await _userManager.CreateAsync(newUser, "@Aa123456");
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(newUser, "Student");
                        var student = new Student
                        {
                            Id = newUser.Id,
                            NameEn = contactUs.Name,
                            NameAr = contactUs.Name,
                            CreatedDate = DateTime.UtcNow
                        };
                        await _unitOfWork.StudentRepository.CreateAsync(student);
                        await _unitOfWork.CommitAsync();
                    }
                }

                TempData["Success"] = isArabic ? "تم إرسال رسالتك بنجاح!" : "Your message has been sent successfully!";
                await _realHub.Clients.All.SendAsync("ReceiveMessage", "New Contact Us Message",
                    $"You have a new message from {contactUs.Name} - {contactUs.Email}");
            }

            return RedirectToAction("Index");
        }

        private async Task<string> GenerateUniqueUsernameAsync(string email)
        {
            var baseName = email.Split('@')[0];

            // sanitize
            baseName = new string(baseName.Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '.').ToArray());
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "user";

            // try a few times
            for (int i = 0; i < 5; i++)
            {
                var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
                var candidate = $"{baseName}_{suffix}";

                var exists = await _userManager.FindByNameAsync(candidate);
                if (exists == null)
                    return candidate;
            }

            // fallback
            return $"{baseName}_{Guid.NewGuid():N}";
        }

    }
}
