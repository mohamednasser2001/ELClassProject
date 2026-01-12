using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Linq.Expressions;
using System.Security.Claims;

namespace ELClass.Areas.Instructor.Controllers
{
    [Area("Instructor")]
    public class StudentController (IUnitOfWork unitOfWork , UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetStudents()
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

                
                Expression<Func<Student, bool>> filter = s =>
                    s.InstructorStudents.Any(e => e.InstructorId == userId) &&
                    (string.IsNullOrEmpty(searchValue) ||
                     s.NameEn.Contains(searchValue) || s.NameAr.Contains(searchValue));

               
                Func<IQueryable<Student>, IOrderedQueryable<Student>> orderBy = q =>
                {
                    if (orderDir == "asc")
                    {
                        return orderColumnIndex switch
                        {
                            "1" => q.OrderBy(s => lang == "en" ? s.NameEn : s.NameAr),
                            _ => q.OrderBy(s => s.Id)
                        };
                    }
                    return orderColumnIndex switch
                    {
                        "1" => q.OrderByDescending(s => lang == "en" ? s.NameEn : s.NameAr),
                        _ => q.OrderBy(s => s.Id)
                    };
                };

           
                var students = await _unitOfWork.StudentRepository.GetAsync(
                    filter: filter,
                    orderBy: orderBy,
                    skip: start,
                    take: length,
                    include:e=>e.Include(e=>e.InstructorStudents)
                );

                
                var totalRecords = await _unitOfWork.StudentRepository.CountAsync(s => s.InstructorStudents.Any(e => e.InstructorId == userId));
                var filteredRecords = await _unitOfWork.StudentRepository.CountAsync(filter: filter);

               
                var data = students.Select(c => new
                {
                    id = c.Id,
                    name = lang == "en" ? c.NameEn : c.NameAr,
                   
                }).ToList();

                return Json(new { draw, recordsTotal = totalRecords, recordsFiltered = filteredRecords, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { draw = Request.Form["draw"].FirstOrDefault(), recordsTotal = 0, recordsFiltered = 0, data = new List<object>(), error = ex.Message });
            }
        }
    }
}
