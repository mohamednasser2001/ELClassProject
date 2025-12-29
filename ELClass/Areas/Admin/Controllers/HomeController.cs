using Microsoft.AspNetCore.Mvc;

namespace ELClass.Areas.Admin.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
