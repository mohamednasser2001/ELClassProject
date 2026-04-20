using AspNetCoreGeneratedDocument;
using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels.Course;
using System.Security.Claims;
using static ELClass.Areas.Instructor.Controllers.CourseController;

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
            if (course == null) return NotFound();

            ViewData["CourseId"] = course.Id;
            ViewData["CourseTitle"] = course.TitleEn;

            var lessons = await unitOfWork.LessonRepository.GetAsync(
                l => l.CourseId == courseId,
                include: l => l.Include(l => l.Instructor)
                               .Include(l => l.LessonAssignments)
                               .Include(l => l.LessonMaterials)
                               .Include(l => l.StudentLessons)
                                   .ThenInclude(sl => sl.Student),
                orderBy: l => l.OrderByDescending(l => l.Id)
            );
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
            var lesson = await unitOfWork.LessonRepository.GetOneAsync(
                l => l.Id == id,
                include: l => l.Include(l => l.Instructor)
                               .Include(l => l.LessonMaterials)
                               .Include(l => l.LessonAssignments)
            );
            if (lesson == null) return View("AdminNotFoundPage");

            var instructors = await unitOfWork.InstructorCourseRepository.GetAsync(
                ic => ic.CourseId == lesson.CourseId,
                include: ic => ic.Include(ic => ic.Instructor)
            );

            ViewBag.Instructors = new SelectList(
                instructors.Select(ic => new { Id = ic.Instructor.Id, Name = ic.Instructor.NameEn }).ToList(),
                "Id", "Name"
            );

            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(Lesson lsn, IFormFileCollection materials, IFormFileCollection assignments)
        {
            ModelState.Remove("Course");
            ModelState.Remove("Instructor");
            ModelState.Remove("LessonMaterials");
            ModelState.Remove("LessonAssignments");

            if (ModelState.IsValid)
            {
                lsn.UpdatedAt = DateTime.Now;

                // لو رفع materials جديدة → احذف القديمة واحطها من جديد
                if (materials != null && materials.Count > 0)
                {
                    var oldMaterials = await unitOfWork.LessonMaterialsRepository
                        .GetAsync(m => m.LessonId == lsn.Id);

                    foreach (var old in oldMaterials)
                    {
                        DeletePhysicalFile(old.FileUrl, "materials");
                        await unitOfWork.LessonMaterialsRepository.DeleteAsync(old);
                    }

                    foreach (var file in materials)
                    {
                        var fileName = await SaveFileAsync(file, "materials");
                        lsn.LessonMaterials.Add(new LessonMaterials { FileUrl = fileName, LessonId = lsn.Id });
                    }
                }

                // لو رفع assignments جديدة → احذف القديمة واحطها من جديد
                if (assignments != null && assignments.Count > 0)
                {
                    var oldAssignments = await unitOfWork.LessonAssignmentsRepository
                        .GetAsync(a => a.LessonId == lsn.Id);

                    foreach (var old in oldAssignments)
                    {
                        DeletePhysicalFile(old.FileUrl, "assignments");
                        await unitOfWork.LessonAssignmentsRepository.DeleteAsync(old);
                    }

                    foreach (var file in assignments)
                    {
                        var fileName = await SaveFileAsync(file, "assignments");
                        lsn.LessonAssignments.Add(new LessonAssignments { FileUrl = fileName, LessonId = lsn.Id });
                    }
                }

                await unitOfWork.LessonRepository.EditAsync(lsn);
                await unitOfWork.CommitAsync();

                TempData["success"] = "Lesson updated successfully";
                return RedirectToAction("GetLessons", "Lesson", new { CourseId = lsn.CourseId });
            }
            return View(lsn);
        }

        private async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folderName);
            Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);
            return uniqueFileName;
        }

        private void DeletePhysicalFile(string fileUrl, string folder)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder, fileUrl);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(int id)
        {
   
            var lesson = await unitOfWork.LessonRepository.GetOneAsync(
                l => l.Id == id,
                include: e=>e.Include(e=>e.LessonAssignments).Include(e=>e.LessonMaterials).Include(e=>e.StudentLessons)   
            );

            if (lesson == null)
            {
                return View("AdminNotFoundPage");
            }

            try
            {
               
          

 
                foreach (var material in lesson.LessonMaterials)
                {
                    DeletePhysicalFile(material.FileUrl , "materials");
                    
                }

                foreach (var assign in lesson.LessonAssignments)
                {
                    DeletePhysicalFile(assign.FileUrl , "assignments");

                }

           
                if (lesson.LessonMaterials.Any())
                    await unitOfWork.LessonMaterialsRepository.DeleteAllAsync(lesson.LessonMaterials);

                if (lesson.LessonAssignments.Any())
                    await unitOfWork.LessonAssignmentsRepository.DeleteAllAsync(lesson.LessonAssignments);

                if (lesson.StudentLessons.Any())
                    await unitOfWork.StudentLessonRepository.DeleteAllAsync(lesson.StudentLessons);

                await unitOfWork.LessonRepository.DeleteAsync(lesson);

           
                await unitOfWork.CommitAsync();

                TempData["success"] = "Lesson and all associated data deleted successfully";
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error while deleting: " + ex.Message;
            }

            return RedirectToAction("GetLessons", "Lesson", new { CourseId = lesson.CourseId });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStudentDegrees([FromBody] List<UpdateDegreeDto> updates)
        {
            try
            {
                foreach (var update in updates)
                {
                    var sl = await unitOfWork.StudentLessonRepository.GetOneAsync(x => x.Id == update.StudentLessonId);
                    if (sl != null)
                    {
                        sl.Degree = update.Degree;
                        await unitOfWork.StudentLessonRepository.EditAsync(sl);
                    }
                }
                await unitOfWork.CommitAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
    }
}
