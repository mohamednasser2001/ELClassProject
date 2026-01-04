using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels.Student;
using System.Threading.Tasks;

namespace ELClass.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StudentController : Controller
    {
        private readonly IUnitOfWork unitOfWork;

        public StudentController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetStudents()
        {
            try
            {

                var draw = Request.Form["draw"].FirstOrDefault();
                var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
                var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
                var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";


                var students = await unitOfWork.StudentRepository.GetAsync();
                var lang = HttpContext.Session.GetString("Language") ?? "en";


                var data = students.Select(c => new
                {
                    id = c.Id,
                    name = lang == "en" ? c.NameEn : c.NameAr,

                }).ToList();


                if (!string.IsNullOrEmpty(searchValue))
                {
                    data = data.Where(c =>
                        (c.name != null && c.name.ToLower().Contains(searchValue.ToLower()))

                    ).ToList();
                }


                var recordsTotal = students.Count();
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


        public async Task<IActionResult> Details(string id)
        {
            var student = unitOfWork.StudentRepository.GetOne(e => e.Id == id, include: e => e.Include(e => e.ApplicationUser));
            if (student == null)
            {
                return View("AdminNotFoundPage");
            }

            var courses = await unitOfWork.StudentCourseRepository.GetAsync(filter: e => e.StudentId == id, include: e => e.Include(e => e.Course));
            var instructors = await unitOfWork.InstructorStudentRepository.GetAsync(filter: e => e.InstructorId == id, include: e => e.Include(e => e.Instructor));
            var model = new StudentDetailsVM()
            {
                Student = student,
                StudentCourses = courses.ToList() ?? new List<StudentCourse>(),
                InstructorStudents = instructors.ToList() ?? new List<InstructorStudent>()
            };
            return View(model);
        }

        public async Task<IActionResult> AssignCourse(int courseId, string studentId)
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

        public async Task<IActionResult> AssignInstructor(string studentId, string instructorId)
        {
            if (studentId != null && instructorId != null)
            {
                var instructorStudent = new InstructorStudent()
                {
                    StudentId = studentId,
                    InstructorId = instructorId
                };
                var res =await unitOfWork.InstructorStudentRepository.CreateAsync(instructorStudent);
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

        public async Task<IActionResult> RemoveCourse(int courseId, string studentId)
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
                    return RedirectToAction("details", new { id = studentId });
                }

            }
            return BadRequest();
        }

        public async Task<IActionResult> RemoveInstructor(string stdId, string insId)
        {
            var instructor = unitOfWork.InstructorStudentRepository.GetOne(filter: e => e.InstructorId == insId && e.StudentId == stdId);
            if (instructor == null)
            {
                return View("AdminNotFoundPage");
            }

            var result = await unitOfWork.InstructorStudentRepository.DeleteAsync(instructor);
            if (result)
            {
                var suc = await unitOfWork.CommitAsync();
                if (suc)
                {

                    return RedirectToAction("details", new { id = insId });
                }

            }
            return BadRequest();
        }
        

        public async Task<IActionResult> SearchInstructors(string term, string studentId)
        {
            var instructors = await unitOfWork.InstructorRepository.GetAsync(filter: e =>
            (e.NameAr.Contains(term) || e.NameEn.Contains(term)));


            var insStdunt = await unitOfWork.InstructorStudentRepository.GetAsync(filter: e => e.StudentId == studentId);
            if (insStdunt.Any())
            {
                var assignedStudentIds = insStdunt.Select(ic => ic.InstructorId).ToList();
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
        public async Task<IActionResult> SearchCourses(string term, string stdId)
        {
            var courses = await unitOfWork.CourseRepository.GetAsync(filter: e =>
            (e.TitleAr.Contains(term) || e.TitleEn.Contains(term)));


            var stdCourses = await unitOfWork.StudentCourseRepository.GetAsync(filter: e => e.StudentId == stdId);
            if (stdCourses.Any())
            {
                var assignedCourseIds = stdCourses.Select(ic => ic.CourseId).ToList();
                courses = courses.Where(c => !assignedCourseIds.Contains(c.Id)).ToList();
            }

            var result = courses
                .Select(c => new
                {
                    id = c.Id,
                    text = $"{c.TitleEn} - {c.TitleAr}"
                });

            return Json(result);
        }
        public IActionResult Create()
        {
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(Student std)
        {
            var lang = HttpContext.Session.GetString("Language") ?? "en";
            ModelState.Remove("ApplicationUser");
            ModelState.Remove("StudentCourses");
            ModelState.Remove("InstructorStudents");
            ModelState.Remove("Id");

            if (!ModelState.IsValid)
            {
                return View(std);
            }

            var newStd = new Student
            {
                Id = "409548af-75f2-49de-852d-b3552166b65d", // محتاجة تتغير بعد ما نعمل ال identity
                NameAr = std.NameAr,
                NameEn = std.NameEn
            };


            await unitOfWork.StudentRepository.CreateAsync(newStd);
            var commit = await unitOfWork.CommitAsync();
            if (!commit)
            {
                ModelState.AddModelError("", "Something went wrong");
                return View(newStd);
            }
            TempData["Success"] = lang == "en" ? " student has been added successfully" : "تمت إضافة الطالب بنجاح";
            return RedirectToAction("Index");

        }

        public IActionResult Edit(string id)
        {
            var student = unitOfWork.StudentRepository.GetOne(i => i.Id == id);
            if (student == null)
            {
                return View("AdminNotFoundPage");
            }
            return View(student);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(Student std)
        {
            var lang = HttpContext.Session.GetString("Language") ?? "en";
            ModelState.Remove("ApplicationUser");
            ModelState.Remove("StudentCourses");
            ModelState.Remove("InstructorStudents");
            if (!ModelState.IsValid)
            {
                return View(std);
            }
            await unitOfWork.StudentRepository.EditAsync(std);
            var commit = await unitOfWork.CommitAsync();
            if (!commit)
            {
                var errorMessage = lang == "en" ? "Something went wrong, Failed To Edit Student" : "حدث خطأ ما، فشل في تعديل الطالب";
                ModelState.AddModelError("", errorMessage);
                return View(std);
            }
            TempData["success"] = lang == "en" ? "Student Edited Successfully" : "تم تعديل الطالب بنجاح";
            return RedirectToAction("Index");
        }





        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var student = unitOfWork.StudentRepository.GetOne(c => c.Id == id);
            if (student == null)
            {
                return View("AdminNotFoundPage");
            }

            await unitOfWork.StudentRepository.DeleteAsync(student);
            await unitOfWork.CommitAsync();
            return RedirectToAction("Index");
        }
    }

}

