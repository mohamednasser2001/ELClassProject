using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels.Instructor;
using System.Threading.Tasks;

namespace ELClass.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class InstructorController : Controller
    {
        private readonly IUnitOfWork unitOfWork;

        public InstructorController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> GetInstructors()
        {
            try
            {

                var draw = Request.Form["draw"].FirstOrDefault();
                var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
                var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
                var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";


                var instructors = await unitOfWork.InstructorRepository.GetAsync();
                var lang = HttpContext.Session.GetString("Language") ?? "en";


                var data = instructors.Select(c => new
                {
                    id = c.Id,
                    name = lang == "en" ? c.NameEn : c.NameAr,
                    bio = lang == "en" ? c.BioEn : c.BioAr
                }).ToList();


                if (!string.IsNullOrEmpty(searchValue))
                {
                    data = data.Where(c =>
                        (c.name != null && c.name.ToLower().Contains(searchValue.ToLower())) ||
                        (c.bio != null && c.bio.ToLower().Contains(searchValue.ToLower()))
                    ).ToList();
                }


                var recordsTotal = instructors.Count();
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
            var instructor = unitOfWork.InstructorRepository.GetOne(e => e.Id == id , include: e=>e.Include(e=>e.ApplicationUser));
            if (instructor == null)
            {
                return View("AdminNotFoundPage");
            }

            var courses = await unitOfWork.InstructorCourseRepository.GetAsync(filter:e=>e.InstructorId==id, include:e=>e.Include(e=>e.Course));
            var students = await unitOfWork.InstructorStudentRepository.GetAsync(filter: e => e.InstructorId == id, include: e => e.Include(e => e.Student));
            var model = new InstructorDetailsVM()
            {
                Instructor = instructor,
                InstructorCourses = courses.ToList() ?? new List<InstructorCourse>(),
                InstructorStudents = students.ToList() ?? new List<InstructorStudent>()
            };
            return View(model);
        }



        public async Task<IActionResult> AssignCourse(int courseId , string instructorId)
        {
            if (courseId != 0 && instructorId != null)
            {
                var instructorCourse = new InstructorCourse()
                {
                    CourseId = courseId,
                    InstructorId = instructorId
                };
                await unitOfWork.InstructorCourseRepository.CreateAsync(instructorCourse);
                await unitOfWork.CommitAsync();
                return Json( new {success = true});

            }
            
                return View("AdminNotFoundPage");
            
        }


        public async Task<IActionResult> AssignStudent(string stdId, string instructorId)
        {
            if (stdId !=null && instructorId != null)
            {
                var instructorStudent = new InstructorStudent()
                {
                    StudentId = stdId,
                    InstructorId = instructorId
                };
                await unitOfWork.InstructorStudentRepository.CreateAsync(instructorStudent);
                await unitOfWork.CommitAsync();
                return Json(new { success = true });

            }
            else
            {
                return View("AdminNotFoundPage");
            }
        }


        public async Task<IActionResult> RemoveCourse(int crsId, string insId)
        {
            var course = unitOfWork.InstructorCourseRepository.GetOne(filter: e => e.InstructorId == insId && e.CourseId == crsId);
            if (course == null)
            {
                return View("AdminNotFoundPage");
            }

            var result = await unitOfWork.InstructorCourseRepository.DeleteAsync(course);
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

        public async Task<IActionResult> RemoveStudent(string stdId, string insId)
        {
            var student = unitOfWork.InstructorStudentRepository.GetOne(filter: e => e.InstructorId == insId && e.StudentId == stdId);
            if (student == null)
            {
                return View("AdminNotFoundPage");
            }

            var result = await unitOfWork.InstructorStudentRepository.DeleteAsync(student);
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

        

        public async Task<IActionResult> SearchStudents(string term, string insId)
        {
            var students = await unitOfWork.StudentRepository.GetAsync(filter: e =>
            (e.NameAr.Contains(term) || e.NameEn.Contains(term)));


            var insStdunt = await unitOfWork.InstructorStudentRepository.GetAsync(filter: e => e.InstructorId == insId);
            if (insStdunt.Any())
            {
                var assignedStudentIds = insStdunt.Select(ic => ic.StudentId).ToList();
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
        public async Task<IActionResult> SearchCourses(string term, string insId)
        {
            var courses = await unitOfWork.CourseRepository.GetAsync(filter: e =>
            (e.TitleAr.Contains(term) || e.TitleEn.Contains(term)));


            var stdCourses = await unitOfWork.InstructorCourseRepository.GetAsync(filter: e => e.InstructorId == insId);
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

        [HttpPost]
        public async Task<IActionResult> Create(Instructor ins)
        {

            var lang = HttpContext.Session.GetString("Language") ?? "en";

            ModelState.Remove("ApplicationUser");
            ModelState.Remove("InstructorStudents");
            ModelState.Remove("InstructorCourses");
            ModelState.Remove("Id");
            if (!ModelState.IsValid)
            {
                return View(ins);
            }
            var newInstructor = new Instructor
            {
                Id = "409548af-75f2-49de-852d-b3552166b65d", // محتاجة تتغير بعد ما نعمل ال identity
                NameAr = ins.NameAr,
                NameEn = ins.NameEn,
                BioAr = ins.BioAr,
                BioEn = ins.BioEn,

            };
            await unitOfWork.InstructorRepository.CreateAsync(newInstructor);
            var commit = await unitOfWork.CommitAsync();
            if (!commit)
            {
                ModelState.AddModelError("", "Something went wrong");
                return View(ins);
            }
            TempData["Success"] = lang == "en" ? " instructor has been added successfully" : "تمت إضافة المدرب بنجاح";
            return RedirectToAction("Index");
        }
        public IActionResult Edit(string id)
        {
            var instructor = unitOfWork.InstructorRepository.GetOne(i => i.Id == id);
            if (instructor == null)
            {
                return View("AdminNotFoundPage");
            }
            return View(instructor);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(Instructor ins)
        {
            var lang = HttpContext.Session.GetString("Language") ?? "en";
            ModelState.Remove("ApplicationUser");
            ModelState.Remove("InstructorStudents");
            ModelState.Remove("InstructorCourses");
            if (!ModelState.IsValid)
            {
                return View(ins);
            }
            await unitOfWork.InstructorRepository.EditAsync(ins);
            var commit = await unitOfWork.CommitAsync();
            if (!commit)
            {
                var errorMessage = lang == "en" ? "Something went wrong, Failed To Edit Instructor" : "حدث خطأ ما، فشل في تعديل المدرب";
                ModelState.AddModelError("", errorMessage);
                return View(ins);
            }
            TempData["success"] = lang == "en" ? "Instructor Edited Successfully" : "تم تعديل المدرب بنجاح";
            return RedirectToAction("Index");
        }
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var instructor = unitOfWork.InstructorRepository.GetOne(c => c.Id == id);
            if (instructor == null)
            {
                return View("AdminNotFoundPage");
            }

            await unitOfWork.InstructorRepository.DeleteAsync(instructor);
            await unitOfWork.CommitAsync();
            return RedirectToAction("Index");
        }
    }
}
