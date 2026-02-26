using DataAccess.Repositories.IRepositories;
using ELClass.services;
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
            var isArabic = CultureHelper.IsArabic;
            var now = DateTime.Now;
            var thirtyDaysAgo = now.AddDays(-30);
            var sixtyDaysAgo = now.AddDays(-60);

            // 1. إحصائيات الطلاب
            var totalStudents = await unitOfWork.StudentRepository.CountAsync();
            var currentMonthNewStudents = await unitOfWork.StudentRepository.CountAsync(s => s.CreatedDate >= thirtyDaysAgo);
            var lastMonthNewStudents = await unitOfWork.StudentRepository.CountAsync(s => s.CreatedDate >= sixtyDaysAgo && s.CreatedDate < thirtyDaysAgo);

            double growthPercentage = 0;
            if (lastMonthNewStudents > 0)
                growthPercentage = ((double)currentMonthNewStudents / lastMonthNewStudents) * 100;
            else if (currentMonthNewStudents > 0)
                growthPercentage = 100;

            ViewBag.TotalStudents = totalStudents;
            ViewBag.newStudents = currentMonthNewStudents;
            ViewBag.StudentsGrowth = Math.Round(growthPercentage, 1);

            // 2. المدرسين والدروس
            ViewBag.ActiveInstructors = await unitOfWork.InstructorRepository.CountAsync();
            ViewBag.NewInstructors = await unitOfWork.InstructorRepository.CountAsync(i => i.CreatedAt >= thirtyDaysAgo);
            ViewBag.NewLessonsThisWeek = await unitOfWork.LessonRepository.CountAsync(l => l.LectureDate >= now.AddDays(-7));
            ViewBag.TotalSubjects = await unitOfWork.CourseRepository.CountAsync();
            // الحصول على آخر 6 أشهر
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.AddMonths(-i))
                .Reverse()
                .ToList();

            var chartLabels = last6Months.Select(d => d.ToString("MMM yyyy")).ToList(); // مثال: "Feb 2026"
            var chartData = new List<int>();

            foreach (var month in last6Months)
            {
                // حساب عدد الطلاب الذين سجلوا في هذا الشهر بالتحديد
                var count = await unitOfWork.StudentRepository.CountAsync(s =>
                    s.CreatedDate.Month == month.Month &&
                    s.CreatedDate.Year == month.Year);

                chartData.Add(count);
            }

            ViewBag.ChartLabels = chartLabels;
            ViewBag.ChartData = chartData;

            // 3. أفضل المواد (Top Subjects)
            var topSubjects = (await unitOfWork.CourseRepository.GetAsync(include: e => e.Include(x => x.StudentCourses)))
                .Select(s => new {
                    Title = isArabic ? s.TitleAr : s.TitleEn, // ضفنا الاسم هنا عشان يظهر في الـ View
                    Count = s.StudentCourses.Count(),
                    Rate = s.StudentCourses.Count() > 0 ? "95%" : "0%"
                })
                .OrderByDescending(s => s.Count)
                .Take(5)
                .ToList();

            return View(topSubjects);
        }






    }
}
