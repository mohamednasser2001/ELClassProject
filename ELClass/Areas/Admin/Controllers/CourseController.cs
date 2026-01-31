using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using ELClass.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels.Course;
using Models.ViewModels.Student;
using System.Globalization;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ELClass.Areas.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    [Area("Admin")]
    public class CourseController : Controller
    {
        private readonly IUnitOfWork unitOfWork;

        public CourseController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        
        public IActionResult Index()
        {
            
            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            var course = await unitOfWork.CourseRepository.GetOneAsync(e => e.Id == id);
            if (course == null)
            {
                return View("AdminNotFoundPage");
            }

            var instructors = await unitOfWork.InstructorCourseRepository.GetAsync(filter: e => e.CourseId == id , e=>e.Include(e=>e.Instructor));
            var students = await unitOfWork.StudentCourseRepository.GetAsync(filter: e => e.CourseId == id, e => e.Include(e => e.Student));
            ViewBag.LessonNumbers = await unitOfWork.LessonRepository.CountAsync(e => e.CourseId == id);
            var model = new CourseDetailsVM()
            {
                Course = course,
                InstructorCourses = instructors.ToList() ,
                StudentCourses = students.ToList()
            };
            return View(model);
        }


        public async Task<IActionResult> SearchStudents(string term, int courseId)
        {
            
            var assignedStudentIds = (await unitOfWork.StudentCourseRepository.GetAsync(filter: e => e.CourseId == courseId))
                                     .Select(ic => ic.StudentId).ToList();

            
            var students = await unitOfWork.StudentRepository.GetAsync(filter: e =>
                (e.NameAr.Contains(term) || e.NameEn.Contains(term)) && !assignedStudentIds.Contains(e.Id));

            var result = students.Select(c => new
            {
                id = c.Id,
                text = $"{c.NameEn} - {c.NameAr}"
            });

            return Json(result);
        }

        public async Task<IActionResult> SearchInstructors(string term, int courseId)
        {
            var instructors = await unitOfWork.InstructorRepository.GetAsync(filter: e =>
            (e.NameAr.Contains(term) || e.NameEn.Contains(term)));


            var insCourse = await unitOfWork.InstructorCourseRepository.GetAsync(filter: e => e.CourseId == courseId);
            if (insCourse.Any())
            {
                var assignedStudentIds = insCourse.Select(ic => ic.InstructorId).ToList();
                instructors = instructors.Where(c => !assignedStudentIds.Contains(c.Id)).ToList();
            }

            var result = instructors
                .Select(c => new
                {
                    id = c.Id,
                    text = $"{c.NameEn} - {c.NameAr}"
                });

            return Json(result);
        }

        public async Task<IActionResult> AssignInstructor(int courseId, string InstructorId)
        {
            if (courseId != 0 && InstructorId != null)
            {
                var InstructorCourse = new InstructorCourse()
                {
                    CourseId = courseId,
                    InstructorId = InstructorId
                };
                var res = await unitOfWork.InstructorCourseRepository.CreateAsync(InstructorCourse);

                if (res)
                {
                    var succ = await unitOfWork.CommitAsync();
                    if (succ)
                    {
                        return Json(new { success = true });
                    }
                }
                
                return Json(new { success = false, message = "There was an error while assigning the instructor to the course." });
            }

            return View("AdminNotFoundPage");
        }

        public async Task<IActionResult> AssignStudent(int courseId, string studentId)
        {
            if (courseId != 0 && studentId != null)
            {
                var studentCourse = new StudentCourse()
                {
                    CourseId = courseId,
                    StudentId = studentId
                };
                var res = await unitOfWork.StudentCourseRepository.CreateAsync(studentCourse);

                if (res)
                {
                    var succ = await unitOfWork.CommitAsync();
                    if (succ)
                    {
                        return Json(new { success = true });
                    }
                }
                return BadRequest();
            }

            return View("AdminNotFoundPage");
        }

        public async Task<IActionResult> RemoveStudent(int courseId, string studentId)
        {
            var studentCourse = await unitOfWork.StudentCourseRepository.GetOneAsync(filter: e => e.StudentId == studentId && e.CourseId == courseId);
            if (studentCourse == null)
            {
                
                return Json(new { success = false, message = "Student is not assigned to this course." });
            }

            var result = await unitOfWork.StudentCourseRepository.DeleteAsync(studentCourse);
            if (result)
            {
                var suc = await unitOfWork.CommitAsync();
                if (suc)
                {
                    return RedirectToAction("Details", "Course", new { id = courseId });
                }
            }

            
            return Json(new { success = false, message = "There was an error removing the student." });
        }

        public async Task<IActionResult> RemoveInstructor(int CourseId, string instructorId)
        {
            var instructor = await unitOfWork.InstructorCourseRepository.GetOneAsync(filter: e => e.InstructorId == instructorId && e.CourseId == CourseId);
            if (instructor == null)
            {
                return Json(new { success = false, message = "Instructor is not assigned to this course." });
            }

            var result = await unitOfWork.InstructorCourseRepository.DeleteAsync(instructor);
            if (result)
            {
                var suc = await unitOfWork.CommitAsync();
                if (suc)
                {
                    return RedirectToAction("Details", "Course", new { id = CourseId });
                }
            }

            return Json(new { success = false, message = "There was an error removing the instructor." });
        }
        [HttpPost]
        public async Task<IActionResult> GetCourseInstructors(int courseId)
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
            var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
            var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";
            var lang = HttpContext.Session.GetString("Language") ?? "en";

            Expression<Func<InstructorCourse, bool>> filter = e => e.CourseId == courseId &&
                (string.IsNullOrEmpty(searchValue) ||
                (lang == "en" ? e.Instructor.NameEn.Contains(searchValue) : e.Instructor.NameAr.Contains(searchValue)));

            var query = await unitOfWork.InstructorCourseRepository.GetAsync(
                filter: filter,
                include: e => e.Include(i => i.Instructor),
                skip: start,
                take: length
            );

            var allDataForCount = await unitOfWork.InstructorCourseRepository.CountAsync(filter: e => e.CourseId == courseId);
            var filteredDataForCount = await unitOfWork.InstructorCourseRepository.CountAsync(filter: filter);

            var result = query.Select(ic => new
            {
                instructorId = ic.InstructorId,
                name = lang == "en" ? ic.Instructor.NameEn : ic.Instructor.NameAr
            }).ToList();

            return Json(new { draw, recordsTotal = allDataForCount, recordsFiltered = filteredDataForCount, data = result });
        }

        [HttpPost]
        public async Task<IActionResult> GetCourseStudents(int courseId)
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
            var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
            var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";
            var lang = HttpContext.Session.GetString("Language") ?? "en";

            Expression<Func<StudentCourse, bool>> filter = e => e.CourseId == courseId &&
                (string.IsNullOrEmpty(searchValue) ||
                (lang == "en" ? e.Student.NameEn.Contains(searchValue) : e.Student.NameAr.Contains(searchValue)));

            var query = await unitOfWork.StudentCourseRepository.GetAsync(
                filter: filter,
                include: e => e.Include(s => s.Student),
                skip: start,
                take: length
            );

            var allDataForCount = await unitOfWork.StudentCourseRepository.CountAsync(filter: e => e.CourseId == courseId);
            var filteredDataForCount = await unitOfWork.StudentCourseRepository.CountAsync(filter: filter);

            var result = query.Select(sc => new
            {
                studentId = sc.StudentId,
                name = lang == "en" ? sc.Student.NameEn : sc.Student.NameAr
            }).ToList();

            return Json(new { draw, recordsTotal = allDataForCount, recordsFiltered = filteredDataForCount, data = result });
        }

        [HttpPost]
        public async Task<IActionResult> GetCourses()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
                var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
                var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";
                var orderColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
                var orderDir = Request.Form["order[0][dir]"].FirstOrDefault();

                
                var lang = CultureInfo.CurrentCulture.Name.StartsWith("ar") ? "ar" : "en";

                Expression<Func<Course, bool>> filter = c => string.IsNullOrEmpty(searchValue) ||
                    (c.TitleEn.Contains(searchValue) || c.TitleAr.Contains(searchValue) ||
                     c.DescriptionEn!.Contains(searchValue) || c.DescriptionAr!.Contains(searchValue));

                Func<IQueryable<Course>, IOrderedQueryable<Course>> orderBy = q =>
                {
                    if (orderDir == "asc")
                    {
                        return orderColumnIndex switch
                        {
                            "1" => q.OrderBy(c => lang == "en" ? c.TitleEn : c.TitleAr),
                            "2" => q.OrderBy(c => lang == "en" ? c.DescriptionEn : c.DescriptionAr),
                            _ => q.OrderBy(c => c.Id)
                        };
                    }
                    return orderColumnIndex switch
                    {
                        "1" => q.OrderByDescending(c => lang == "en" ? c.TitleEn : c.TitleAr),
                        "2" => q.OrderByDescending(c => lang == "en" ? c.DescriptionEn : c.DescriptionAr),
                        _ => q.OrderByDescending(c => c.Id)
                    };
                };

                var courses = await unitOfWork.CourseRepository.GetAsync(
                    filter: filter,
                    orderBy: orderBy,
                    skip: start,
                    take: length
                );

                var totalRecords = await unitOfWork.CourseRepository.CountAsync();
                var filteredRecords = await unitOfWork.CourseRepository.CountAsync(filter: filter);

                
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
        public IActionResult Create()
        {
            return View();
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(Course crs)
        {
            ModelState.Remove("StudentCourses");
            ModelState.Remove("InstructorCourses");

           
            bool isArabic = CultureHelper.IsArabic;

            if (!ModelState.IsValid)
            {
                return View(crs);
            }

            var course = new Course()
            {
                TitleAr = crs.TitleAr,
                TitleEn = crs.TitleEn,
                DescriptionAr = crs.DescriptionAr,
                DescriptionEn = crs.DescriptionEn,
                CreatedAt = DateTime.Now,
                CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            await unitOfWork.CourseRepository.CreateAsync(course);
            var commit = await unitOfWork.CommitAsync();

            if (!commit)
            {
                string errorMessage = isArabic ? "حدث خطأ ما أثناء إضافة الكورس" : "Something went wrong while adding the course";
                ModelState.AddModelError("", errorMessage);
                return View(crs);
            }

            TempData["success-notifications"] = isArabic ? "تم إضافة الكورس بنجاح" : "Course created successfully.";

            return RedirectToAction("Index");
        }


        public async Task<IActionResult> Edit(int id)
        {
            var course = await unitOfWork.CourseRepository.GetOneAsync(c => c.Id == id);
            if (course == null)
            {
                return View("AdminNotFoundPage");
            }
            return View(course);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(Course crs)
        {
            ModelState.Remove("StudentCourses");
            ModelState.Remove("InstructorCourses");

            
            
            bool isArabic = CultureHelper.IsArabic;

            if (!ModelState.IsValid)
            {
                return View(crs);
            }

            crs.UpdatedAt = DateTime.Now;

            await unitOfWork.CourseRepository.EditAsync(crs);
            var commit = await unitOfWork.CommitAsync();

            if (!commit)
            {
                string errorMessage = isArabic ? "حدث خطأ ما أثناء الحفظ" : "Something went wrong during save";
                ModelState.AddModelError("", errorMessage);
                return View(crs);
            }

            TempData["success-notifications"] = isArabic ? "تم تحديث الكورس بنجاح" : "Course updated successfully.";

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id) 
        {
            var course = await unitOfWork.CourseRepository.GetOneAsync(c => c.Id == id);
            if (course == null)
            {
                return View("AdminNotFoundPage");
            }
               var transaction = await unitOfWork.BeginTransactionAsync();
            try
            {

               
                var relatedLessons = await unitOfWork.LessonRepository.GetAsync(c => c.CourseId == id);
                if (relatedLessons.Any())
                {
                    await unitOfWork.LessonRepository.DeleteAllAsync(relatedLessons);
                }

                
                
                await unitOfWork.CourseRepository.DeleteAsync(course);

                await unitOfWork.CommitAsync();
                await transaction.CommitAsync();
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync() ; 
                                                  
                return View("Error");
            }
        }
    }
}