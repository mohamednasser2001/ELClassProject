using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;

namespace ELClass.Areas.StudentArea.Controllers
{
    [Area("StudentArea")]
    public class CourseController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourseController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            this._userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);


            bool isEnrolled = await _unitOfWork.StudentCourseRepository.GetOneAsync(
                sc => sc.StudentId == userId && sc.CourseId == id
            ) != null;

            if (!isEnrolled)
            {
                return Forbid();
            }

       



           
            var course = await _unitOfWork.CourseRepository.GetOneAsync(
                e => e.Id == id,
                q => q.Include(c => c.Lessons)
            );

            if (course == null)
            {
                return NotFound();
            }

            return View(course); 
        }

        public async Task<IActionResult> LessonDetails(int id)
        {
            var userId = _userManager.GetUserId(User);
           
            var lesson = await _unitOfWork.LessonRepository.GetOneAsync(l => l.Id == id);
            if (lesson == null) return NotFound();

      
            bool isEnrolled = _unitOfWork.StudentCourseRepository.GetOneAsync(e =>
                e.StudentId == userId && e.CourseId == lesson.Id) != null;

            if (!isEnrolled) return Forbid();

          

            return View(lesson);
        }

     
    }
}
