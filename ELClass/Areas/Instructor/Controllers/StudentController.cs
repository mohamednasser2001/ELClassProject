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
                var isArabic = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ar";
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var draw = Request.Form["draw"].FirstOrDefault();
                var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
                var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
                var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";
                var orderColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
                var orderDir = Request.Form["order[0][dir]"].FirstOrDefault();
                var lang = isArabic ? "ar" : "en";

                
                Expression<Func<InstructorStudent, bool>> filter = x =>
                    x.InstructorId == userId &&
                    (string.IsNullOrEmpty(searchValue) ||
                     x.Student.NameEn.Contains(searchValue) ||
                     x.Student.NameAr.Contains(searchValue));

                
                Func<IQueryable<InstructorStudent>, IOrderedQueryable<InstructorStudent>> orderBy = q =>
                {
                    if (orderDir == "asc")
                    {
                        return orderColumnIndex switch
                        {
                            "1" => q.OrderBy(x => lang == "en" ? x.Student.NameEn : x.Student.NameAr),
                            _ => q.OrderBy(x => x.StudentId)
                        };
                    }
                    return orderColumnIndex switch
                    {
                        "1" => q.OrderByDescending(x => lang == "en" ? x.Student.NameEn : x.Student.NameAr),
                        _ => q.OrderByDescending(x => x.StudentId)
                    };
                };

                
                var instructorStudents = await _unitOfWork.InstructorStudentRepository.GetAsync(
                    filter: filter,
                    orderBy: orderBy,
                    skip: start,
                    take: length,
                    include: q => q.Include(x => x.Student) 
                );

                var totalRecords = await _unitOfWork.InstructorStudentRepository.CountAsync(x => x.InstructorId == userId);
                var filteredRecords = await _unitOfWork.InstructorStudentRepository.CountAsync(filter: filter);

                var data = instructorStudents.Select(x => new
                {
                    id = x.Student.Id, 
                    name = lang == "en" ? x.Student.NameEn : x.Student.NameAr,
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
