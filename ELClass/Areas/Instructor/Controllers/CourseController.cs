using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ELClass.Areas.Instructor.Controllers
{
    [Area("Instructor")]
     [Authorize(Roles = "Instructor")]
    public class CourseController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourseController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            this._unitOfWork = unitOfWork;
            this._userManager = userManager;
        }
        public IActionResult ChangeLanguage(string lang)
        {

            HttpContext.Session.SetString("Language", lang);


            return Redirect(Request.Headers["Referer"].ToString());
        }
        public async Task<IActionResult> Index()
        {
            return View();

        }

        [HttpPost]
        public async Task<IActionResult> GetCourses()
        {
            try
            {

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var draw = Request.Form["draw"].FirstOrDefault();
                var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
                var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
                var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";
                var orderColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
                var orderDir = Request.Form["order[0][dir]"].FirstOrDefault();
                var lang = HttpContext.Session.GetString("Language") ?? "en";


                Expression<Func<Course, bool>> filter = c =>
                    c.InstructorCourses.Any(ic => ic.InstructorId == userId) &&
                    (string.IsNullOrEmpty(searchValue) ||
                    (c.TitleEn.Contains(searchValue) || c.TitleAr.Contains(searchValue) ||
                     c.DescriptionEn!.Contains(searchValue) || c.DescriptionAr!.Contains(searchValue)));


                Func<IQueryable<Course>, IOrderedQueryable<Course>> orderBy = q =>
                {
                    if (orderDir == "asc")
                    {
                        return orderColumnIndex switch
                        {
                            "1" => q.OrderBy(c => lang == "en" ? c.TitleEn : c.TitleAr),
                            "2" => q.OrderBy(c => lang == "en" ? c.DescriptionEn : c.DescriptionAr),
                            _ => q.OrderBy(c => c.CreatedAt)
                        };
                    }
                    
                    return orderColumnIndex switch
                    {
                        "1" => q.OrderByDescending(c => lang == "en" ? c.TitleEn : c.TitleAr),
                        "2" => q.OrderByDescending(c => lang == "en" ? c.DescriptionEn : c.DescriptionAr),
                        _ => q.OrderByDescending(c => c.CreatedAt)
                    };
                };

                var courses = await _unitOfWork.CourseRepository.GetAsync(
                    filter: filter,
                    orderBy: orderBy,
                    skip: start,
                    take: length,
                    include: e => e.Include(e => e.InstructorCourses)
                );


                var totalRecords = await _unitOfWork.CourseRepository.CountAsync(c => c.InstructorCourses.Any(ic => ic.InstructorId == userId));
                var filteredRecords = await _unitOfWork.CourseRepository.CountAsync(filter: filter);

                var result = courses.Select(c => new
                {
                    id = c.Id,
                    title = lang == "en" ? c.TitleEn : c.TitleAr,
                    description = lang == "en" ? c.DescriptionEn : c.DescriptionAr,

                }).ToList();

                return Json(new { draw, recordsTotal = totalRecords, recordsFiltered = filteredRecords, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { draw = Request.Form["draw"].FirstOrDefault(), recordsTotal = 0, recordsFiltered = 0, data = new List<object>(), error = ex.Message });
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var course = await _unitOfWork.CourseRepository.GetOneAsync(c => c.Id == id);
            if (course == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = course.Id;
            ViewData["CourseTitle"] = course.TitleEn;
            var insId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var lessons = await _unitOfWork.LessonRepository.GetAsync(l => l.CourseId == id && l.InstructorId == insId);
            return View(lessons);
        }

        public async Task<IActionResult> GetStudents(int id)
        {
            var course = await _unitOfWork.CourseRepository.GetOneAsync(c => c.Id == id);
            if (course == null) return NotFound();

            ViewData["CourseId"] = course.Id;
            ViewData["CourseTitle"] = course.TitleEn;

            
            var registrations = await _unitOfWork.StudentCourseRepository.GetAsync(
                filter: r => r.CourseId == id,
                include: e=>e.Include(e=>e.Student).ThenInclude(e=>e.ApplicationUser) 
            );

            return View(registrations);
        }

        public IActionResult createLesson(int CourseId)
        {

            return View(new Lesson() { CourseId = CourseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> createLesson(Lesson lsn)
        {
            ModelState.Remove("Course");
            ModelState.Remove("Instructor");
            if (ModelState.IsValid)
            {
                lsn.CreatedAt = DateTime.Now;
                lsn.CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
                lsn.InstructorId= User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                await _unitOfWork.LessonRepository.CreateAsync(lsn);
                await _unitOfWork.CommitAsync();
                TempData["success"] = "Lesson created successfully";
                return RedirectToAction("Details", new { id = lsn.CourseId });
            }
            return View(lsn);
        }

        public async Task<IActionResult> EditLesson(int id)
        {
            var lesson = await _unitOfWork.LessonRepository.GetOneAsync(l => l.Id == id);
            if (lesson == null)
            {
                return NotFound();
            }
            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(Lesson lsn)
        {
            ModelState.Remove("Course");
            ModelState.Remove("Instructor");
            if (ModelState.IsValid)
            {
                lsn.UpdatedAt = DateTime.Now;
                lsn.InstructorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                await _unitOfWork.LessonRepository.EditAsync(lsn);
                await _unitOfWork.CommitAsync();
                TempData["success"] = "Lesson updated successfully";
                return RedirectToAction("Details", new { id = lsn.CourseId });
            }
            return View(lsn);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var lesson = await _unitOfWork.LessonRepository.GetOneAsync(l => l.Id == id);
            if (lesson == null)
            {
                return View("AdminNotFoundPage");
            }
            await _unitOfWork.LessonRepository.DeleteAsync(lesson);
            await _unitOfWork.CommitAsync();
            TempData["success"] = "Lesson deleted successfully";
            return RedirectToAction("Details", new { id = lesson.CourseId });
        }
    }
}
