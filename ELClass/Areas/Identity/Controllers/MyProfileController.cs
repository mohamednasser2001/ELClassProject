using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels.Student;

namespace ELClass.Areas.Identity.Controllers
{
    [Area("Identity")]
    [Authorize]
    public class MyProfileController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public MyProfileController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            this._unitOfWork = unitOfWork;
            this._userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }
      
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new StudentProfileVM
            {
                Id = user.Id,
                FullName = user.NameEN ?? user.UserName ?? user.Email ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.Img
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyProfile(StudentProfileVM model)
        {
           
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

           
            ModelState.Remove(nameof(StudentProfileVM.ProfileImageUrl));
            ModelState.Remove(nameof(StudentProfileVM.Initials));
            ModelState.Remove(nameof(StudentProfileVM.Email));


            if (!string.Equals(model.Id, user.Id, StringComparison.Ordinal))
                return Forbid();

            if (!ModelState.IsValid)
            {
                model.Email = user.Email ?? "";
                model.ProfileImageUrl = user.Img;
                return View(model);
            }

            try
            {
           
                user.NameEN = model.FullName;
                user.PhoneNumber = model.PhoneNumber;

                if (User.IsInRole("Student"))
                {
                    var studentInDb = await _unitOfWork.StudentRepository.GetOneAsync(
                        e => e.Id == user.Id,
                        query => query.Include(e => e.ApplicationUser)
                    );

                    if (studentInDb != null)
                    {
                       
                        studentInDb.NameEn = model.FullName;
                    }
                }
                if (User.IsInRole("Instructor"))
                {
                    var instructorInDb = await _unitOfWork.InstructorRepository.GetOneAsync(
                        i => i.Id == user.Id,
                        query => query.Include(i => i.ApplicationUser)
                    );

                    if (instructorInDb != null)
                    {
                        instructorInDb.NameEn = model.FullName;

                        
                    }
                }

         
                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(model.ProfilePicture.FileName);
                    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "users");
                    Directory.CreateDirectory(folderPath);

                    var filePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfilePicture.CopyToAsync(stream);
                    }

                  
                    if (!string.IsNullOrEmpty(user.Img))
                    {
                        var oldPath = Path.Combine(folderPath, user.Img);
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    user.Img = fileName;
                }

       
                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                    if (!passResult.Succeeded)
                    {
                        foreach (var error in passResult.Errors)
                            ModelState.AddModelError("", error.Description);

                        model.Email = user.Email ?? "";
                        model.ProfileImageUrl = user.Img;
                        return View(model);
                    }
                }

    
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                        ModelState.AddModelError("", error.Description);

                    model.Email = user.Email ?? "";
                    model.ProfileImageUrl = user.Img;
                    return View(model);
                }

                await _unitOfWork.CommitAsync();

                TempData["Success"] = "Profile updated successfully";
                return RedirectToAction(nameof(MyProfile));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                model.Email = user.Email ?? "";
                model.ProfileImageUrl = user.Img;
                return View(model);
            }
        }
    }
}
   

