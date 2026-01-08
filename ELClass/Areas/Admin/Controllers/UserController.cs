using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;

namespace ELClass.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
                var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
                var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";


                var query = _userManager.Users;


                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(u => u.UserName!.Contains(searchValue) ||
                                             u.Email!.Contains(searchValue) ||
                                             u.PhoneNumber.Contains(searchValue));
                }


                var totalRecords = await _userManager.Users.CountAsync();
                var filteredRecords = await query.CountAsync();



                var allUsers = await query
                    .OrderBy(u => u.UserName)
                    .Skip(start)
                    .Take(length)
                    .ToListAsync();

                var users = new List<object>();
                foreach (var u in allUsers)
                {

                    var roles = await _userManager.GetRolesAsync(u);
                    users.Add(new
                    {
                        id = u.Id,
                        userName = u.UserName,
                        email = u.Email,
                        phoneNumber = u.PhoneNumber ?? "N/A",
                        role = roles.Any() ? string.Join(',', roles) : "No Role"
                    });
                }

                return Json(new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredRecords,
                    data = users
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        public IActionResult Details(string id)
        {
            var user = _unitOfWork.InstructorRepository.GetOneAsync(u => u.Id == id);
            if (user == null)
            {
                return RedirectToAction("Details", "Student" ,new { id = id });
            }
            return RedirectToAction("Details", "Instructor", new { id = id });
        }


    }
}
