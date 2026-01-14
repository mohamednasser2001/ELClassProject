using System.Threading.Tasks;
using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using Models.ViewModels.Student;

namespace ELClass.Areas.StudentArea.Controllers
{
    [Area("StudentArea")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            this._unitOfWork = unitOfWork;
            this._userManager = userManager;
        }
   
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // 1. الكود بتاعك (بيجيب الكورسات اللي الطالب مشترك فيها)
          
            var courses = await _unitOfWork.StudentCourseRepository.GetAsync(e => e.StudentId == userId, q => q.Include(e => e.Course));
            var coursesVM = courses.Select(sc => new StudentCoursesVM
            {
                CourseId = sc.CourseId,
                CourseTitleEn = sc.Course.TitleEn,
                CourseTitleAr = sc.Course.TitleAr
            }).ToList();
            // 2. هنجيب "كل المدرسين" من الداتا بيز عشان الشات
            var allInstructors = await _unitOfWork.InstructorRepository.GetAsync();
            var instructorsVM = allInstructors.Select(i => new InstructorChatVM
            {
                InstructorId = i.Id.ToString(),
                InstructorNameEn = i.NameEn,
<<<<<<< HEAD
                InstructorNameAr = i.NameAr
=======
                InstructorNameAr = i.NameAr,
                InstructorImage = i.img
>>>>>>> 21059d53a3fcba0dcba9805a914dd4af4ec8f05b
            }).ToList();
            var model = new StudentDashboardVM
            {
                Courses = coursesVM,
                Instructors = instructorsVM
            };

            return View(model);

        }

        [HttpPost]
        public async Task<IActionResult> SaveMessage(string receiverId, string content)
        {
            // الحصول على ID الطالب الحالي
            var senderId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(receiverId))
            {
                return BadRequest("Invalid message data");
            }

            var newMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = content,
                Timestamp = DateTime.Now,
                IsRead = false
            };

            try
            {
                // استخدام الـ UnitOfWork للوصول للـ Repository وإضافة الرسالة
                await _unitOfWork.ChatMessageRepository.CreateAsync(newMessage);

                // حفظ التغييرات عن طريق الـ UnitOfWork
                var result = await _unitOfWork.CommitAsync();

                if (result)
                    return Ok(new { success = true });

                return BadRequest("Failed to save the message");
            }
            catch (Exception ex)
            {
                // يفضل هنا عمل Log للـ ex.Message
                return StatusCode(500, "Internal server error occurred while saving the message");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetChatHistory(string instructorId)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(instructorId)) return BadRequest();

            // جلب الرسائل بين الطرفين (أنا اللي باعت أو أنا اللي مستلم)
            var messages = await _unitOfWork.ChatMessageRepository.GetAsync(
                m => (m.SenderId == currentUserId && m.ReceiverId == instructorId) ||
                     (m.SenderId == instructorId && m.ReceiverId == currentUserId)
            );

            // ترتيب الرسائل من الأقدم للأحدث عشان تظهر منطقية في الشات
            var sortedMessages = messages.OrderBy(m => m.Timestamp).ToList();

            return Json(sortedMessages);
        }


        [HttpPost]
        public async Task<IActionResult> MarkAsRead(string instructorId)
        {
            var currentUserId = _userManager.GetUserId(User);

            // هات كل الرسايل اللي المدرس بعتها لي وأنا لسه مشفتهاش
            var unreadMessages = await _unitOfWork.ChatMessageRepository.GetAsync(
                m => m.SenderId == instructorId && m.ReceiverId == currentUserId && !m.IsRead
            );

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }

            await _unitOfWork.CommitAsync();
            return Ok();
        }

<<<<<<< HEAD
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new StudentProfileVM
            {
                Id = user.Id,
                FullName = user.NameEN,       
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.Img   
            };

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyProfile(StudentProfileVM model)
        {
          
            ModelState.Remove(nameof(StudentProfileVM.ProfileImageUrl));
            ModelState.Remove(nameof(StudentProfileVM.Initials));

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var studentInDb = await _unitOfWork.StudentRepository.GetOneAsync(
                    e => e.Id == model.Id,
                    query => query.Include(e => e.ApplicationUser)
                );

                if (studentInDb == null)
                    return NotFound();

                
                studentInDb.NameEn = model.FullName;

                if (studentInDb.ApplicationUser != null)
                {
                    var user = studentInDb.ApplicationUser;

                   
                    user.PhoneNumber = model.PhoneNumber;
                    user.NameEN = model.FullName;   
                    user.UserName = user.Email;

                    if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(model.ProfilePicture.FileName);
                        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "users");

                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);

                        var filePath = Path.Combine(folderPath, fileName);

                        using var stream = new FileStream(filePath, FileMode.Create);
                        await model.ProfilePicture.CopyToAsync(stream);

                        
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
                        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                        if (!result.Succeeded)
                        {
                            foreach (var error in result.Errors)
                                ModelState.AddModelError("", error.Description);

                            return View(model);
                        }
                    }
                }

                await _unitOfWork.CommitAsync();

                TempData["Success"] = "Profile updated successfully";
                return RedirectToAction(nameof(MyProfile));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return View(model);
            }
        }


=======
>>>>>>> 21059d53a3fcba0dcba9805a914dd4af4ec8f05b

    }
}
