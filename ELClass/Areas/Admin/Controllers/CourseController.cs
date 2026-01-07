using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels.Course;
using Models.ViewModels.Student;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ELClass.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class CourseController : Controller
    {
        private readonly IUnitOfWork unitOfWork;

        public CourseController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            
            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            var course = unitOfWork.CourseRepository.GetOne(e => e.Id == id);
            if (course == null)
            {
                return View("AdminNotFoundPage");
            }

            var instructors = await unitOfWork.InstructorCourseRepository.GetAsync(filter: e => e.CourseId == id , e=>e.Include(e=>e.Instructor));
            var students = await unitOfWork.StudentCourseRepository.GetAsync(filter: e => e.CourseId == id, e => e.Include(e => e.Student));
            var model = new CourseDetailsVM()
            {
                Course = course,
                InstructorCourses = instructors.ToList() ,
                StudentCourses = students.ToList()
            };
            return View(model);
        }

        public async Task<IActionResult> SearchStudents(string term, int crsId)
        {
            var students = await unitOfWork.StudentRepository.GetAsync(filter: e =>
            (e.NameAr.Contains(term) || e.NameEn.Contains(term)));


            var crsStdunt = await unitOfWork.StudentCourseRepository.GetAsync(filter: e => e.CourseId == crsId);
            if (crsStdunt.Any())
            {
                var assignedStudentIds = crsStdunt.Select(ic => ic.StudentId).ToList();
                students = students.Where(c => !assignedStudentIds.Contains(c.Id)).ToList();
            }

            var result = students
                .Select(c => new
                {
                    id = c.Id,
                    text = $"{c.NameEn} - {c.NameAr}"
                });

            return Json(result);
        }

        public async Task<IActionResult> SearchInstructors(string term, int crsId)
        {
            var instructors = await unitOfWork.InstructorRepository.GetAsync(filter: e =>
            (e.NameAr.Contains(term) || e.NameEn.Contains(term)));


            var insCourse = await unitOfWork.InstructorCourseRepository.GetAsync(filter: e => e.CourseId == crsId);
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
                return BadRequest();
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
            var course = unitOfWork.StudentCourseRepository.GetOne(filter: e => e.StudentId == studentId && e.CourseId == courseId);
            if (course == null)
            {
                return View("AdminNotFoundPage");
            }

            var result = await unitOfWork.StudentCourseRepository.DeleteAsync(course);
            if (result)
            {
                var suc = await unitOfWork.CommitAsync();
                if (suc)
                {
                    return RedirectToAction("details", "course", new { id = courseId });
                }

            }
            return BadRequest();
        }
        public async Task<IActionResult> RemoveInstructor(int CourseId, string instructorId)
        {
            var instructor = unitOfWork.InstructorCourseRepository.GetOne(filter: e => e.InstructorId == instructorId && e.CourseId == CourseId);
            if (instructor == null)
            {
                return View("AdminNotFoundPage");
            }

            var result = await unitOfWork.InstructorCourseRepository.DeleteAsync(instructor);
            if (result)
            {
                var suc = await unitOfWork.CommitAsync();
                if (suc)
                {

                    return RedirectToAction("details", "course" ,new { id = CourseId });
                }

            }
            return BadRequest();
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


                var courses = await unitOfWork.CourseRepository.GetAsync();
                var lang = HttpContext.Session.GetString("Language") ?? "en";


                var data = courses.Select(c => new
                {
                    id = c.Id,
                    title = lang == "en" ? c.TitleEn : c.TitleAr,
                    description = lang == "en" ? c.DescriptionEn : c.DescriptionAr
                }).ToList();


                if (!string.IsNullOrEmpty(searchValue))
                {
                    data = data.Where(c =>
                        (c.title != null && c.title.ToLower().Contains(searchValue.ToLower())) ||
                        (c.description != null && c.description.ToLower().Contains(searchValue.ToLower()))
                    ).ToList();
                }


                var recordsTotal = courses.Count();
                var recordsFiltered = data.Count();


                var result = data
                    .Skip(start)
                    .Take(length)
                    .ToList();

                return Json(new
                {
                    draw = draw,
                    recordsTotal = recordsTotal,
                    recordsFiltered = recordsFiltered,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = Request.Form["draw"].FirstOrDefault(),
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = ex.Message
                });
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
                CreateAT = DateTime.Now,
                CreateById = User.FindFirstValue(ClaimTypes.NameIdentifier)
                //UpdatedAT = DateTime.Now,
            };
            await unitOfWork.CourseRepository.CreateAsync(course);
            var commit = await unitOfWork.CommitAsync();
            if (!commit)
            {
                ModelState.AddModelError("", "Something went wrong");
                return View(crs);
            }
            return RedirectToAction("Index");
        }


        public IActionResult Edit(int id)
        {
            var course = unitOfWork.CourseRepository.GetOne(c => c.Id == id);
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

            if (!ModelState.IsValid)
            {
                return View(crs);
            }
            crs.UpdatedAT = DateTime.Now;
            await unitOfWork.CourseRepository.EditAsync(crs);
            var commit = await unitOfWork.CommitAsync();
            if (!commit)
            {
                ModelState.AddModelError("", "Something went wrong");
                return View(crs);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id) 
        {
            var course = unitOfWork.CourseRepository.GetOne(c => c.Id == id);
            if (course == null)
            {
                return View("AdminNotFoundPage");
            }

            await unitOfWork.CourseRepository.DeleteAsync(course);
            await unitOfWork.CommitAsync();
            return RedirectToAction("Index");
        }
    }
}