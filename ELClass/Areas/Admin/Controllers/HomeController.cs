using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ELClass.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork unitOfWork;

        public HomeController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        public async Task<IActionResult> Index()
        {
            // مثال لجلب البيانات - قم بتغيير الاستعلامات حسب أسماء الجداول لديك
            ViewBag.TotalStudents = await unitOfWork.StudentRepository.CountAsync();
            ViewBag.StudentsGrowth = 12; // احسب النسبة البرمجية هنا

            //ViewBag.ActiveLessons = await unitOfWork.LessonRepository.CountAsync(l => l.IsActive);
            ViewBag.newStudents = await unitOfWork.StudentRepository.CountAsync(i => i.CreatedDate >= DateTime.Now.AddDays(-30));
            ViewBag.NewLessonsThisWeek = await unitOfWork.LessonRepository.CountAsync(l => l.LectureDate >= DateTime.Now.AddDays(-7));

            ViewBag.TotalSubjects = await unitOfWork.CourseRepository.CountAsync();

            ViewBag.ActiveInstructors = await unitOfWork.InstructorRepository.CountAsync();
            ViewBag.NewInstructors = await unitOfWork.InstructorRepository.CountAsync(i => i.CreatedAt >= DateTime.Now.AddDays(-30));

            // جلب قائمة المواد الأعلى أداءً (مثال)
            var topSubjects = (await unitOfWork.CourseRepository.GetAsync())
                .Select(s => new {
                    
                   
                    TitleAr = s.TitleAr,
                    TitleEn = s.TitleEn,
                    Count = s.StudentCourses.Count(),
                    Rate = "90%"
                })
                .OrderByDescending(s => s.Count)
                .Take(5)
                .ToList();

            return View(topSubjects);
        }




    }
}
