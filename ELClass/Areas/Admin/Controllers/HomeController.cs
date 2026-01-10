using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELClass.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork unitOfWork;

        public HomeController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            
            return View();
        }

        [HttpPost]
        public IActionResult SetLanguage(string language)
        {

            HttpContext.Session.SetString("Language", language);

            return Json(new { success = true });
        }
    }
}
