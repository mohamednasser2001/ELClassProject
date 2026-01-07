using System.Threading.Tasks;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;

namespace ELClass.Areas.StudentArea.Controllers
{
    [Area("StudentArea")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            this._unitOfWork = unitOfWork;
            this._userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

      
            var courses = await _unitOfWork.StudentCourseRepository.GetAsync(e => e.StudentId == userId,q=>q.Include(e=>e.Course));

            var viewModel = courses.Select(sc => new StudentCoursesVM
            {
                CourseId = sc.CourseId,
                CourseTitleEn = sc.Course.TitleEn,
                CourseTitleAr = sc.Course.TitleAr
            });

            return View(viewModel);
           
        }
    }
}
