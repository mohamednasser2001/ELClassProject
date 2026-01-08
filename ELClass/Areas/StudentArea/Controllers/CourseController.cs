using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using Models;
using Microsoft.EntityFrameworkCore;

namespace ELClass.Areas.StudentArea.Controllers
{
    [Area("StudentArea")]
    public class CourseController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CourseController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Details(int id)
        {


            var course = _unitOfWork.CourseRepository.GetOneAsync(e => e.Id == id, q => q.Include(c => c.Lessons));


            if (course == null)
            {
                return NotFound();
            }

            return View(course); // هنبعت موديل الكورس للـ View
        }
    }
}
