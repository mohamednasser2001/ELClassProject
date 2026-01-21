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
                return Json(new { conversationId = (int?)null, hasMore = false, messages = Array.Empty<object>() });

            var msgs = (await _unitOfWork.CHMessageRepository.GetAsync(
                m => m.ConversationId == convo.Id && (beforeId == null || m.Id < beforeId),
                tracked: false,
                orderBy: q => q.OrderByDescending(m => m.Id),
                take: take + 1
            )).ToList();

            bool hasMore = msgs.Count > take;
            if (hasMore) msgs.RemoveAt(msgs.Count - 1);

            msgs.Reverse();

            var list = msgs.Select(m => new
            {
                id = m.Id,
                senderId = m.SenderId,
                receiverId = m.ReceiverId,
                content = m.Content,
                sentAt = m.SentAt,
                isRead = m.IsRead
            });

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

            // 1) هات كل المدرسين اللي الطالب متعملهم Assign
            var links = await _unitOfWork.InstructorStudentRepository.GetAsync(
                x => x.StudentId == studentId,
                tracked: false
            );

            if (links == null || !links.Any())
                return Json(Array.Empty<object>());

            var instructorIds = links
                .Select(x => x.InstructorId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            // 2) هات الـ conversations (لو موجودة) بين الطالب والمدرسين دول
            var convos = await _unitOfWork.ConversationRepository.GetAsync(
                c => c.StudentId == studentId && instructorIds.Contains(c.InstructorId),
                tracked: false
            );

            // map: InstructorId -> Conversation (آخر واحدة)
            var convoByInstructor = convos
                .GroupBy(c => c.InstructorId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.LastMessageAt).First());

            // 3) هات أسماء المدرسين من AspNetUsers (ApplicationUser)
            var instructors = await _userManager.Users
                .Where(u => instructorIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.UserName }) // لو عندك NameEn/FullName حطها هنا
                .ToListAsync();

            var nameMap = instructors.ToDictionary(x => x.Id, x => x.Name);

            // 4) ابني النتيجة: المدرس يظهر حتى لو مفيش Conversation
            var result = instructorIds.Select(instructorId =>
            {
                convoByInstructor.TryGetValue(instructorId, out var c);

                // fallback للترتيب: وقت الـ assign لو مفيش رسائل
                var assignedAt = links.FirstOrDefault(x => x.InstructorId == instructorId)?.CreatedAt
                                 ?? DateTime.MinValue;

                return new
                {
                    id = c?.Id,
                    instructorId = instructorId,
                    instructorName = nameMap.ContainsKey(instructorId) ? nameMap[instructorId] : "Instructor",
                    lastMessage = c?.LastMessagePreview ?? "",
                    lastMessageAt = c?.LastMessageAt ?? assignedAt,
                    unread = c?.UnreadForStudent ?? 0
                };
            })
            .OrderByDescending(x => x.lastMessageAt)
            .ToList();

            return Json(result);
        }




     




    }
}
