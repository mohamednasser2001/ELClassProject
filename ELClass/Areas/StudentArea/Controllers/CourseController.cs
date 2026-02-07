using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;

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
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // 1) هات كورسات الطالب
            var courses = await _unitOfWork.StudentCourseRepository
                .GetAsync(e => e.StudentId == userId, q => q.Include(e => e.Course));

            // 2) هات المواعيد عشان نحسب الـ Progress
            var studentAppointments = await _unitOfWork.StudentAppointmentRepository
                .GetAsync(sa => sa.StudentId == userId, q => q.Include(sa => sa.Appointment));

            var attendedCountByCourseId = studentAppointments
                .Where(sa => sa.Appointment != null && sa.IsAttended)
                .GroupBy(sa => sa.Appointment!.CourseId)
                .ToDictionary(g => g.Key, g => g.Count());

            const int defaultGoal = 8;

            // 3) تحويل البيانات للـ ViewModel (StudentCoursesVM)
            var coursesVM = courses.Select(sc =>
            {
                var attended = attendedCountByCourseId.TryGetValue(sc.CourseId, out var c) ? c : 0;

                return new StudentCoursesVM
                {
                    CourseId = sc.CourseId,
                    CourseTitleEn = sc.Course.TitleEn,
                    CourseTitleAr = sc.Course.TitleAr,
                    AttendedCount = attended,
                    GoalCount = defaultGoal
                    // ProgressPercent بيتحسب أوتوماتيك داخل الـ VM لو أنت عامله كدة
                };
            }).ToList();

            // بنبعث قائمة الكورسات مباشرة للـ View
            return View(coursesVM);
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
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            // هات الـ Lesson
            var lesson = await _unitOfWork.LessonRepository.GetOneAsync(l => l.Id == id);
            if (lesson == null) return NotFound();

            // تأكد إن الطالب مشترك في كورس الـ Lesson (مهم: lesson.CourseId مش lesson.Id)
            var enrollment = await _unitOfWork.StudentCourseRepository.GetOneAsync(e =>
                e.StudentId == userId && e.CourseId == lesson.CourseId
            );

            if (enrollment == null) return Forbid();

            return View(lesson);
        }



    }
}
