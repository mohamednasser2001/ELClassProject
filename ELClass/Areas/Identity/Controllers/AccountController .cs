using Microsoft.AspNetCore.Mvc;

namespace ELClass.Areas.Identity.Controllers
{
    [Area("Identity")]
    public class AccountController : Controller
    {
        public IActionResult Register()
        {
            return View();
        }
    }
}
