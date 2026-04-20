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

        public CourseController(IUnitOfWork _unitOfWork, UserManager<ApplicationUser> userManager)
        {
            this._unitOfWork = _unitOfWork;
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
            var lessons = await _unitOfWork.LessonRepository.GetAsync(l => l.CourseId == id && l.InstructorId == insId ,
                l=>l.Include(l=>l.LessonMaterials).Include(l=>l.LessonAssignments)
                .Include(e=>e.StudentLessons).ThenInclude(e=>e.Student));
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
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLesson(Lesson lsn, IFormFileCollection materials, IFormFileCollection assignments)
        {
            ModelState.Remove("Course");
            ModelState.Remove("Instructor");
            ModelState.Remove("LessonMaterials");
            ModelState.Remove("LessonAssignments");

            if (ModelState.IsValid)
            {
                lsn.CreatedAt = DateTime.Now;
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                lsn.CreatedById = userId;
                lsn.InstructorId = userId!;


                if (materials != null && materials.Count > 0)
                {
                    foreach (var file in materials)
                    {
                        var fileName = await SaveFileAsync(file, "materials");
                        lsn.LessonMaterials.Add(new LessonMaterials { FileUrl = fileName });
                    }
                }

            
                if (assignments != null && assignments.Count > 0)
                {
                    foreach (var file in assignments)
                    {
                        var fileName = await SaveFileAsync(file, "assignments");
                        lsn.LessonAssignments.Add(new LessonAssignments { FileUrl = fileName });
                    }
                }

                await _unitOfWork.LessonRepository.CreateAsync(lsn);
                await _unitOfWork.CommitAsync();

                TempData["success"] = "Lesson and files created successfully";
                return RedirectToAction("Details", new { id = lsn.CourseId });
            }
            return View(lsn);
        }



        [HttpPost]
        public async Task<IActionResult> CreateLesson2(Lesson lsn, IFormFileCollection materials,
        IFormFileCollection assignments, [FromForm] List<string> studentIds,
        [FromForm] List<double> degrees)
            {
                ModelState.Remove("Course");
                ModelState.Remove("Instructor");
                ModelState.Remove("LessonMaterials");
                ModelState.Remove("LessonAssignments");

                if (ModelState.IsValid)
                {
                    lsn.CreatedAt = DateTime.Now;
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    lsn.CreatedById = userId;
                    lsn.InstructorId = userId!;

                    if (materials != null && materials.Count > 0)
                        foreach (var file in materials)
                            lsn.LessonMaterials.Add(new LessonMaterials { FileUrl = await SaveFileAsync(file, "materials") });

                    if (assignments != null && assignments.Count > 0)
                        foreach (var file in assignments)
                            lsn.LessonAssignments.Add(new LessonAssignments { FileUrl = await SaveFileAsync(file, "assignments") });

                    // لو مفيش studentIds جاية من الفورم، جيب طلاب الـ appointment
                    if (!studentIds.Any())
                    {
                        var appointmentId = int.Parse(Request.Form["lessonAppointmentId"].FirstOrDefault() ?? "0");
                        if (appointmentId > 0)
                        {
                            var appointment = await _unitOfWork.AppoinmentRepository.GetOneAsync(
                                a => a.Id == appointmentId,
                                include: e => e.Include(e => e.StudentAppointments)
                            );
                            if (appointment != null)
                                studentIds = appointment.StudentAppointments.Select(sa => sa.StudentId).ToList();
                        }
                    }

                    // كل الطلاب بياخدوا درجتهم أو صفر لو مش محددة
                    for (int i = 0; i < studentIds.Count; i++)
                    {
                        lsn.StudentLessons.Add(new StudentLesson
                        {
                            StudentId = studentIds[i],
                            Degree = i < degrees.Count ? degrees[i] : 0  // صفر تلقائي
                        });
                    }

                    await _unitOfWork.LessonRepository.CreateAsync(lsn);
                    await _unitOfWork.CommitAsync();

                    return Json(new { success = true, message = "Lesson created successfully" });
                }

                return Json(new { success = false, message = "Validation failed" });
            }



        private async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folderName);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return uniqueFileName; 
        }


        [HttpPost]
        public async Task<IActionResult> UpdateStudentDegrees([FromBody] List<UpdateDegreeDto> updates)
        {
            try
            {
                foreach (var update in updates)
                {
                    var sl = await _unitOfWork.StudentLessonRepository.GetOneAsync(x => x.Id == update.StudentLessonId);
                    if (sl != null)
                    {
                        sl.Degree = update.Degree;
                        await _unitOfWork.StudentLessonRepository.EditAsync(sl);
                    }
                }
                await _unitOfWork.CommitAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class UpdateDegreeDto
        {
            public int StudentLessonId { get; set; }
            public double Degree { get; set; }
        }



        public async Task<IActionResult> EditLesson(int id)
        {
            var lesson = await _unitOfWork.LessonRepository.GetOneAsync(l => l.Id == id ,e=>e.Include(e=>e.LessonMaterials).Include(e=>e.LessonAssignments));
            if (lesson == null)
            {
                return NotFound();
            }
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
                lsn.InstructorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            
                if (materials != null && materials.Count > 0)
                {
                    var oldMaterials = await _unitOfWork.LessonMaterialsRepository
                        .GetAsync(m => m.LessonId == lsn.Id);

                    foreach (var old in oldMaterials)
                    {
                        DeletePhysicalFile(old.FileUrl, "materials");
                        await _unitOfWork.LessonMaterialsRepository.DeleteAsync(old);
                    }

                    foreach (var file in materials)
                    {
                        var fileName = await SaveFileAsync(file, "materials");
                        lsn.LessonMaterials.Add(new LessonMaterials { FileUrl = fileName, LessonId = lsn.Id });
                    }
                }

         
                if (assignments != null && assignments.Count > 0)
                {
                    var oldAssignments = await _unitOfWork.LessonAssignmentsRepository
                        .GetAsync(a => a.LessonId == lsn.Id);

                    foreach (var old in oldAssignments)
                    {
                        DeletePhysicalFile(old.FileUrl, "assignments");
                        await _unitOfWork.LessonAssignmentsRepository.DeleteAsync(old);
                    }

                    foreach (var file in assignments)
                    {
                        var fileName = await SaveFileAsync(file, "assignments");
                        lsn.LessonAssignments.Add(new LessonAssignments { FileUrl = fileName, LessonId = lsn.Id });
                    }
                }

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

            var lesson = await _unitOfWork.LessonRepository.GetOneAsync(
                l => l.Id == id,
                include: e => e.Include(e => e.LessonAssignments).Include(e => e.LessonMaterials).Include(e => e.StudentLessons)
            );

            try
            {




                foreach (var material in lesson.LessonMaterials)
                {
                    DeletePhysicalFile(material.FileUrl, "materials");

                }

                foreach (var assign in lesson.LessonAssignments)
                {
                    DeletePhysicalFile(assign.FileUrl, "assignments");

                }


                if (lesson.LessonMaterials.Any())
                    await _unitOfWork.LessonMaterialsRepository.DeleteAllAsync(lesson.LessonMaterials);

                if (lesson.LessonAssignments.Any())
                    await _unitOfWork.LessonAssignmentsRepository.DeleteAllAsync(lesson.LessonAssignments);

                if (lesson.StudentLessons.Any())
                    await _unitOfWork.StudentLessonRepository.DeleteAllAsync(lesson.StudentLessons);

                await _unitOfWork.LessonRepository.DeleteAsync(lesson);


                await _unitOfWork.CommitAsync();

                TempData["success"] = "Lesson and all associated data deleted successfully";
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error while deleting: " + ex.Message;
            }

            return RedirectToAction("Details", "Course", new { id = lesson.CourseId });
        }


        private void DeletePhysicalFile(string fileUrl, string folder)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;


            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder, fileUrl);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}
