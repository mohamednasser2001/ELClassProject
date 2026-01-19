using System.Threading.Tasks;
using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
            var allInstructors = await _unitOfWork.InstructorRepository.GetAsync(null, q => q.Include(i => i.ApplicationUser));

            var instructorsVM = allInstructors.Select(i => new InstructorChatVM
            {
                InstructorId = i.Id.ToString(),
                InstructorNameEn = i.NameEn,
                InstructorNameAr = i.NameAr,
                ApplicationUser = i.ApplicationUser

            }).ToList();
            var model = new StudentDashboardVM
            {
                Courses = coursesVM,
                Instructors = instructorsVM
            };

            return View(model);

        }

        //chat
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveMessage(string receiverId, string content)
        {
            var senderId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(senderId))
                return Unauthorized();

            content = content?.Trim();

            if (string.IsNullOrWhiteSpace(receiverId) || string.IsNullOrWhiteSpace(content))
                return Json(new { success = false, message = "Invalid data" });

            // ✅ 0) تأكد إن الـ receiver موجود (على الأقل)
            var receiverExists = await _userManager.FindByIdAsync(receiverId) != null;
            if (!receiverExists)
                return Json(new { success = false, message = "Receiver not found" });

            // ✅ 1) GetOrCreate Conversation بشكل آمن
            Conversation? convo =await  _unitOfWork.ConversationRepository
                .GetOneAsync(c => c.StudentId == senderId && c.InstructorId == receiverId, tracked: true);

            if (convo == null)
            {
                convo = new Conversation
                {
                    StudentId = senderId,
                    InstructorId = receiverId,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow,
                    LastMessagePreview = "",
                    LastMessageSenderId = senderId,
                    UnreadForStudent = 0,
                    UnreadForInstructor = 0
                };

                try
                {
                    await _unitOfWork.ConversationRepository.CreateAsync(convo);
                    await _unitOfWork.CommitAsync(); // مهم عشان ناخد convo.Id
  
                }
                catch
                {
                    // ✅ لو حصلت Race / Unique index hit: هاتها تاني
                    convo = await _unitOfWork.ConversationRepository
                        .GetOneAsync(c => c.StudentId == senderId && c.InstructorId == receiverId, tracked: true);

                    if (convo == null)
                        return Json(new { success = false, message = "Failed to create conversation" });
                }
            }

            // ✅ 2) احفظ الرسالة
            var now = DateTime.UtcNow;
            var msg = new CHMessage
            {
                ConversationId = convo.Id,
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                SentAt = now,
                IsRead = false
            };

            await _unitOfWork.CHMessageRepository.CreateAsync(msg);

            // ✅ 3) حدّث Conversation
            convo.LastMessageAt = now;
            convo.LastMessagePreview = content.Length > 50 ? content.Substring(0, 50) : content;
            convo.LastMessageSenderId = senderId;

            // sender = الطالب -> زوّد unread عند المدرس
            convo.UnreadForInstructor = (convo.UnreadForInstructor < 0) ? 1 : convo.UnreadForInstructor + 1;

            await _unitOfWork.CommitAsync();

            return Json(new
            {
                success = true,
                conversationId = convo.Id,
                message = new
                {
                    id = msg.Id,
                    senderId = msg.SenderId,
                    receiverId = msg.ReceiverId,
                    content = msg.Content,
                    sentAt = msg.SentAt,
                    isRead = msg.IsRead
                }
            });
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetChatHistory(string instructorId, int take = 10, long? beforeId = null)
        {
            var studentId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(studentId)) return Unauthorized();
            if (string.IsNullOrWhiteSpace(instructorId)) return BadRequest();
            if (take < 1 || take > 50) take = 10;

            var convo = await _unitOfWork.ConversationRepository
                .GetOneAsync(c => c.StudentId == studentId && c.InstructorId == instructorId, tracked: false);

            if (convo == null)
                return Json(new { conversationId = 0, hasMore = false, messages = Array.Empty<object>() });

            var msgs = (await _unitOfWork.CHMessageRepository.GetAsync(
                m => m.ConversationId == convo.Id && (beforeId == null || m.Id < beforeId),
                tracked: false,
                orderBy: q => q.OrderByDescending(m => m.Id),
                take: take + 1
            )).ToList();

            bool hasMore = msgs.Count > take;
            if (hasMore) msgs.RemoveAt(msgs.Count - 1);

            var list = msgs
                .OrderBy(m => m.Id)
                .Select(m => new {
                    id = m.Id,
                    senderId = m.SenderId,
                    receiverId = m.ReceiverId,
                    content = m.Content,
                    sentAt = m.SentAt,
                    isRead = m.IsRead
                })
                .ToList();

            return Json(new { conversationId = convo.Id, hasMore, messages = list });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkAsRead(string instructorId)
        {
            var studentId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(studentId)) return Unauthorized();
            if (string.IsNullOrWhiteSpace(instructorId)) return BadRequest();

            var convo = await _unitOfWork.ConversationRepository
                .GetOneAsync(c => c.StudentId == studentId && c.InstructorId == instructorId, tracked: true);

            if (convo == null) return Ok();

            var now = DateTime.UtcNow;

            var unreadMessages = await _unitOfWork.CHMessageRepository.GetAsync(
                m => m.ConversationId == convo.Id &&
                     m.ReceiverId == studentId &&
                     !m.IsRead,
                tracked: true
            );

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
                msg.ReadAt = now;
            }

            // ✅ خلّي العداد يعكس الحقيقة (تجنب race conditions)
            // لو عندك CountAsync في الريبو استخدمه، لو مش موجود هنسيبه 0 مؤقتًا
            convo.UnreadForStudent = 0;

            await _unitOfWork.CommitAsync();

            return Ok();
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyConversations()
        {
            var studentId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(studentId)) return Unauthorized();

            var convos = await _unitOfWork.ConversationRepository.GetAsync(
                c => c.StudentId == studentId,
                tracked: false,
                orderBy: q => q.OrderByDescending(c => c.LastMessageAt)
            );

            if (!convos.Any())
                return Json(Array.Empty<object>());

            var instructorIds = convos.Select(c => c.InstructorId).Distinct().ToList();

            // ✅ هنا التعديل المهم: هات بيانات المدرس من AspNetUsers
            var instructors = await _userManager.Users
                .Where(u => instructorIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.UserName }) // غيّر UserName لو عندك NameEn
                .ToListAsync();

            var map = instructors.ToDictionary(x => x.Id, x => x.Name);

            return Json(convos.Select(c => new {
                id = c.Id,
                instructorId = c.InstructorId,
                instructorName = map.ContainsKey(c.InstructorId) ? map[c.InstructorId] : "Instructor",
                lastMessage = c.LastMessagePreview,
                lastMessageAt = c.LastMessageAt,
                unread = c.UnreadForStudent
            }));
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
                FullName = user.NameEN!,       
                Email = user.Email!,
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




    }
}
