using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ViewModels.Instructor;
using System.Security.Claims;

namespace ELClass.Areas.Instructor.Controllers
{
    [Area("Instructor")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var viewModel = new InstructorIndexDashboardVM
            {
                // 1. عدد الطلاب المرتبطين بهذا المدرس
                TotalStudents = (await _unitOfWork.InstructorStudentRepository
                                .CountAsync(isd => isd.InstructorId == userId)),

                // 2. عدد الدروس (Lessons) في الكورسات التي يدرسها
                ActiveLessons = (await _unitOfWork.LessonRepository
                                .CountAsync(l => l.Course.InstructorCourses.Any(ic => ic.InstructorId == userId))),

                // 3. عدد الكورسات التي يشارك فيها المدرس
                TotalCourses = (await _unitOfWork.InstructorCourseRepository
                               .CountAsync(ic => ic.InstructorId == userId)),

                // 4. إجمالي المدرسين في النظام (اختياري)
                TotalInstructors = (await _unitOfWork.InstructorRepository.CountAsync()),

                // 5. بيانات للكورسات الأعلى أداءً (أول 5 كورسات مثلاً)
                TopPerformingCourses = (await _unitOfWork.CourseRepository.GetAsync(
                    c => c.CreatedById == userId,
                    take: 5)).Select(c => new CourseProgressVM
                    {
                        CourseName = c.TitleEn,
                        EnrolledStudents = 0, // يمكنك ربطها بجدول StudentCourses لاحقاً
                        SuccessRate = 90 // قيمة افتراضية
                    }).ToList()
            };

            return View(viewModel);
        }
    }
}
