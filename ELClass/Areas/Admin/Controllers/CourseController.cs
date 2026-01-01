using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.ViewModels.Course;
using System.Threading.Tasks;

namespace ELClass.Areas.Admin.Controllers
{
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

        [HttpPost]
        public IActionResult SetLanguage(string language)
        {
            
            HttpContext.Session.SetString("Language", language);

            return Json(new { success = true });
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
                CreateById = "409548af-75f2-49de-852d-b3552166b65d", // محتاجة تتغير بعد ما نعمل ال identity
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