using DataAccess.Repositories;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace ELClass.Areas.Admin.Controllers
{
   
    public class CourseController : Controller
    {
       
        public IActionResult Index()
        {
            return View();
        }
    }
}
