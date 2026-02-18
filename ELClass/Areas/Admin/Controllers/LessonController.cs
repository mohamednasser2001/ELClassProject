using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels.Course;
using System.Security.Claims;

namespace ELClass.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class LessonController(IUnitOfWork unitOfWork) : Controller
    {
        private readonly IUnitOfWork unitOfWork = unitOfWork;

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetLessons(int courseId)
        {
            var course = await unitOfWork.CourseRepository.GetOneAsync(c => c.Id == courseId);
            if (course == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = course.Id;
            ViewData["CourseTitle"] = course.TitleEn;
            var insId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var lessons = await unitOfWork.LessonRepository.GetAsync(l => l.CourseId == courseId, include: l => l.Include(l => l.Instructor), orderBy: l => l.OrderByDescending(l => l.Id));
            return View(lessons);
        }

        public async Task<IActionResult> CreateLesson(int courseId)
        {

            var instructors = await unitOfWork.InstructorCourseRepository.GetAsync(
                ic => ic.CourseId == courseId,
                include: ic => ic.Include(ic => ic.Instructor)
            );


            var instructorList = instructors.Select(ic => new
            {
                Id = ic.Instructor.Id,
                Name = ic.Instructor.NameEn
            }).ToList();


            ViewBag.Instructors = new SelectList(instructorList, "Id", "Name");

            return View(new Lesson() { CourseId = courseId });
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

                await unitOfWork.LessonRepository.CreateAsync(lsn);
                await unitOfWork.CommitAsync();
                TempData["success"] = "Lesson created successfully";
                return RedirectToAction("GetLessons", "Lesson", new { CourseId = lsn.CourseId });
            }
            return View(lsn);
        }


        public async Task<IActionResult> EditLesson(int id)
        {
            var lesson = await unitOfWork.LessonRepository.GetOneAsync(l => l.Id == id, include: l => l.Include(l => l.Instructor));
            if (lesson == null) return View("AdminNotFoundPage");
            var instructors = await unitOfWork.InstructorCourseRepository.GetAsync(
                ic => ic.CourseId == lesson.CourseId,
                include: ic => ic.Include(ic => ic.Instructor)
            );


            var instructorList = instructors.Select(ic => new
            {
                Id = ic.Instructor.Id,
                Name = ic.Instructor.NameEn
            }).ToList();


            ViewBag.Instructors = new SelectList(instructorList, "Id", "Name");

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
                await unitOfWork.LessonRepository.EditAsync(lsn);
                await unitOfWork.CommitAsync();
                TempData["success"] = "Lesson updated successfully";
                return RedirectToAction("GetLessons", "Lesson", new { CourseId = lsn.CourseId });
            }
            return View(lsn);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var lesson = await unitOfWork.LessonRepository.GetOneAsync(l => l.Id == id);
            if (lesson == null)
            {
                return View("AdminNotFoundPage");
            }
            await unitOfWork.LessonRepository.DeleteAsync(lesson);
            await unitOfWork.CommitAsync();
            TempData["success"] = "Lesson deleted successfully";
            return RedirectToAction("GetLessons", "Lesson", new { CourseId = lesson.CourseId });
        }
    }
}
