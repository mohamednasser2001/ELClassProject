using DataAccess.Repositories.IRepositories;
using ELClass.Hubs;
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

        public async Task<IActionResult> Index()
        {
            var instructors = await _unitOfWork.InstructorRepository.GetAsync(include: e => e.Include(e => e.ApplicationUser));
            HomePageIndexVM model = new HomePageIndexVM() { Instructors = instructors.ToList() };
            return View(model);
            //return RedirectToAction("Login", "Account", new { area = "Identity" });
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
            if (ModelState.IsValid)
            {
                await _unitOfWork.ContactUsRepository.CreateAsync(contactUs);
                await _unitOfWork.CommitAsync();
                await _realHub.Clients.All.SendAsync("ReceiveMessage", "New Contact Us Message", $"You have a new message from {contactUs.Name} - {contactUs.Email}");
            }
            return RedirectToAction("Index");

        }

    }
}
