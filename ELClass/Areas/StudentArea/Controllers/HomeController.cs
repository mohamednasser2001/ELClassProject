using System.Threading.Tasks;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;

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
                InstructorNameAr = i.NameAr,
                InstructorImage = i.img
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


    }
}
