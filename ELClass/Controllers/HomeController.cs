using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using System.Threading.Tasks;

namespace ELClass.Controllers
{

    public class HomeController(IUnitOfWork unitOfWork) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

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
            }
            return RedirectToAction("Index");

        }

    }
}
