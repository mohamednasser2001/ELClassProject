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
        public async Task<IActionResult> Details(int id, int? openLessonId = null)
        {
            var userId = _userManager.GetUserId(User);

            // 1) تأكد إن الطالب enrolled في الكورس
            var isEnrolled = await _unitOfWork.StudentCourseRepository.GetOneAsync(
                sc => sc.StudentId == userId && sc.CourseId == id
            );
            if (isEnrolled == null) return Forbid();

            // 2) هات المدرس بتاع الطالب (اللي مربوط بيه) - ممكن يكون null
            var insLink = await _unitOfWork.InstructorStudentRepository.GetOneAsync(
                x => x.StudentId == userId
            );

            string? instructorId = insLink?.InstructorId;

            // Flags للـ View
            ViewBag.HasInstructor = (instructorId != null);
            ViewBag.AssignedInstructorId = instructorId;

            // 3) هات الكورس بدون Lessons
            var course = await _unitOfWork.CourseRepository.GetOneAsync(
                c => c.Id == id
            );
            if (course == null) return NotFound();

            // 4) هات Lessons للكورس بتاعة المدرس ده فقط (ولو مفيش مدرس => فاضي)
            var lessons = new List<Lesson>();

            if (instructorId != null)
            {
                var result = await _unitOfWork.LessonRepository.GetAsync(
                    l => l.CourseId == id && l.InstructorId == instructorId,
                    tracked: false,
                    orderBy: q => q.OrderBy(l => l.LectureDate)
                );

                lessons = result?.ToList() ?? new List<Lesson>();
            }

            course.Lessons = lessons;

            ViewBag.OpenLessonId = openLessonId;
            return View(course);
        }



        public async Task<IActionResult> LessonDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var lesson = await _unitOfWork.LessonRepository.GetOneAsync(
                l => l.Id == id,
                q => q.Include(x => x.Course),
                tracked: false
            );

            if (lesson == null) return NotFound();

            var isEnrolled = await _unitOfWork.StudentCourseRepository
                .GetOneAsync(e => e.StudentId == userId && e.CourseId == lesson.CourseId, tracked: false);

            if (isEnrolled == null) return Forbid();

            return View(lesson);
        }




    }
}
