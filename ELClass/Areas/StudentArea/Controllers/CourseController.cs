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
            bool isEnrolled = _unitOfWork.StudentCourseRepository.GetOne(sc =>
            sc.StudentId == userId && sc.CourseId == id) != null;

            if (!isEnrolled)
            {
                // لو مش مشترك، نرجعه لصفحة ممنوع الدخول أو الصفحة الرئيسية
                return Forbid();
            }

            var course = _unitOfWork.CourseRepository.GetOneAsync(e => e.Id == id, q => q.Include(c => c.Lessons));


            if (course == null)
            {
                return NotFound();
            }

            return View(course); // هنبعت موديل الكورس للـ View
        }

        public IActionResult  LessonDetails(int id)
        {
            var userId = _userManager.GetUserId(User);
            // بنجيب الدرس بناءً على الـ Id بتاعه
            var lesson =  _unitOfWork.LessonRepository.GetOne(l => l.Id == id);
            if (lesson == null) return NotFound();

            // نتحقق إن الطالب ده مسجل في الكورس اللي بيتبع له الدرس ده
            bool isEnrolled = _unitOfWork.StudentCourseRepository.GetOne(e =>
                e.StudentId == userId && e.CourseId == lesson.CourseId) != null;

            if (!isEnrolled) return Forbid();

          

            return View(lesson);
        }
    }
}
