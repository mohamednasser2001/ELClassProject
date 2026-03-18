using DataAccess.Repositories.IRepositories;
using ELClass.Hubs;
using ELClass.services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using System.Threading.Tasks;

namespace ELClass.Controllers
{

    public class HomeController(IUnitOfWork unitOfWork , IHubContext<RealTimeHub> realHub) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IHubContext<RealTimeHub> _realHub = realHub;

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
            HomePageIndexVM model = new HomePageIndexVM()
            {
                Instructors = instructors.ToList(),
                Content = content,
                SelectedCountry = selectedCountry
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
                TempData["Success"] = isArabic ? "تم إرسال رسالتك بنجاح!" : "Your message has been sent successfully!";
                await _realHub.Clients.All.SendAsync("ReceiveMessage", "New Contact Us Message", $"You have a new message from {contactUs.Name} - {contactUs.Email}");
            }
            //TempData["Error"] = isArabic ? "حدث خطأ أثناء إرسال رسالتك. الرجاء المحاولة مرة أخرى." : "An error occurred while sending your message. Please try again.";
            return RedirectToAction("Index");

        }

    }
}
