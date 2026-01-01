using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;

namespace ELClass.Areas.Admin.Controllers
{
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
    }
}
