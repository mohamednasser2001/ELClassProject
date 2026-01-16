using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using System.Threading.Tasks;

namespace ELClass.Areas.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
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


                var query = _userManager.Users.Where(e=>e.UserName !="SuperAdmin123");


                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query.Where(u => u.UserName!.Contains(searchValue) ||
                                             u.Email!.Contains(searchValue) ||
                                             u.PhoneNumber!.Contains(searchValue));
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
                    var isBlocked = await _userManager.IsLockedOutAsync(u);
                    var roles = await _userManager.GetRolesAsync(u);
                    users.Add(new
                    {
                        id = u.Id,
                        userName = u.UserName,
                        email = u.Email,
                        phoneNumber = u.PhoneNumber ?? "N/A",
                        role = roles.Any() ? string.Join(',', roles) : "No Role",
                        isBlocked = isBlocked
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
                return RedirectToAction("Details", "Student", new { id = id });
            }
            return RedirectToAction("Details", "Instructor", new { id = id });
        }

        [HttpPost] 
        public async Task<IActionResult> ToggleBlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return Json(new { success = false, message = "User not found" });


            var isBlocked = await _userManager.IsLockedOutAsync(user);
            IdentityResult result;

            if (isBlocked)
            {

                result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
                if (result.Succeeded) await _userManager.ResetAccessFailedCountAsync(user);
            }
            else
            {

                await _userManager.SetLockoutEnabledAsync(user, true);
                result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                if (result.Succeeded) await _userManager.UpdateSecurityStampAsync(user);
            }

            if (result.Succeeded)
            {
                return Json(new
                {
                    success = true,
                    message = isBlocked ? "User Unblocked" : "User Blocked",
                    newStatus = !isBlocked
                });
            }

            return Json(new { success = false, message = "Error updating status" });
        }

        public async Task<IActionResult> Block(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return View("AdminNotFoundPage");
            }
            await _userManager.SetLockoutEnabledAsync(user, true);
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                TempData["success"] = "User has been blocked successfully";

            }
            else
                TempData["Error"] = "Failed To Block User";

            return RedirectToAction("index");
        }
        public async Task<IActionResult> Unblock(String id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return View("AdminNotFoundPage");
            }
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTime.UtcNow);
            if (result.Succeeded)
            {
                await _userManager.ResetAccessFailedCountAsync(user);

                TempData["success"] = "User has been unblocked successfully";

            }
            else
            {
                TempData["Error"] = "Failed To Unblock User";
            }
            return RedirectToAction("index");
        }
        [HttpGet]
        public async Task<IActionResult> ManageRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return View("AdminNotFoundPage");


            var allRoles = new List<string> { "Admin", "SuperAdmin", };


            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new ManageUserRolesVM
            {
                UserId = id,
                UserName = user.UserName!,

                SelectedRole = userRoles.FirstOrDefault(r => r == "Admin" || r == "SuperAdmin") ?? "None"
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(string userId, string selectedRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Json(new { success = false });

            
            var adminRoles = new[] { "Admin", "SuperAdmin" };

            
            var currentRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in adminRoles)
            {
                if (currentRoles.Contains(role))
                {
                    await _userManager.RemoveFromRoleAsync(user, role);
                }
            }

            
            if (selectedRole != "None")
            {
                await _userManager.AddToRoleAsync(user, selectedRole);
            }

            return RedirectToAction("Index");
        }
    }
}
