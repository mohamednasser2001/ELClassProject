using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using Models;
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
            TempData["Success"] = lang== "en" ? " student has been added successfully" : "تمت إضافة الطالب بنجاح";
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

