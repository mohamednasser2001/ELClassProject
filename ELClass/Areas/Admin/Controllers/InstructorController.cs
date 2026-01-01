using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using Models;
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
