using System.Security.Claims;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels.Instructor;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ELClass.Areas.Instructor.Controllers
{
    [Area("Instructor")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var viewModel = new InstructorIndexDashboardVM
            {
                // 1. عدد الطلاب المرتبطين بهذا المدرس
                TotalStudents = (await _unitOfWork.InstructorStudentRepository
                                .CountAsync(isd => isd.InstructorId == userId)),

                // 2. عدد الدروس (Lessons) في الكورسات التي يدرسها
                ActiveLessons = (await _unitOfWork.LessonRepository
                                .CountAsync(l => l.Course.InstructorCourses.Any(ic => ic.InstructorId == userId))),

                // 3. عدد الكورسات التي يشارك فيها المدرس
                TotalCourses = (await _unitOfWork.InstructorCourseRepository
                               .CountAsync(ic => ic.InstructorId == userId)),

                // 4. إجمالي المدرسين في النظام (اختياري)
                TotalInstructors = (await _unitOfWork.InstructorRepository.CountAsync()),

                // 5. بيانات للكورسات الأعلى أداءً (أول 5 كورسات مثلاً)
                TopPerformingCourses = (await _unitOfWork.CourseRepository.GetAsync(
                    c => c.CreatedById == userId,
                    take: 5)).Select(c => new CourseProgressVM
                    {
                        CourseName = c.TitleEn,
                        EnrolledStudents = 0, // يمكنك ربطها بجدول StudentCourses لاحقاً
                        SuccessRate = 90 // قيمة افتراضية
                    }).ToList()

            };

         
            viewModel.Students = (await _unitOfWork.InstructorStudentRepository.GetAsync(
                       e => e.InstructorId == userId, e => e.Include(x => x.Student).ThenInclude(s => s.ApplicationUser)
                       )).Select(e => e.Student).ToList();




            return View(viewModel);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetInstructorConversations()
        {
            var instructorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(instructorId)) return Unauthorized();

            // 1) هات كل الطلاب المرتبطين بالمدرس
            var links = await _unitOfWork.InstructorStudentRepository.GetAsync(
                x => x.InstructorId == instructorId,
                tracked: false
            );

            var studentIds = links.Select(x => x.StudentId).Distinct().ToList();

            if (studentIds.Count == 0)
                return Json(Array.Empty<object>());

            // 2) هات بيانات الطلاب من AspNetUsers
            var users = await _userManager.Users
                .Where(u => studentIds.Contains(u.Id))
                .ToListAsync();

            var usersMap = users.ToDictionary(u => u.Id, u => u.UserName);

            // 3) هات المحادثات الموجودة
            var convos = await _unitOfWork.ConversationRepository.GetAsync(
                c => c.InstructorId == instructorId && studentIds.Contains(c.StudentId),
                tracked: false,
                orderBy: q => q.OrderByDescending(c => c.LastMessageAt)
            );

            var convoMap = convos.ToDictionary(c => c.StudentId, c => c);

            // 4) رجّع كل الطلاب حتى لو مفيش رسائل
            var result = studentIds.Select(sid =>
            {
                convoMap.TryGetValue(sid, out var c);

                return new
                {
                    id = c?.Id ?? 0,
                    studentId = sid,
                    studentName = usersMap.ContainsKey(sid) ? usersMap[sid] : "Student",
                    lastMessage = c?.LastMessagePreview ?? "",
                    lastMessageAt = c?.LastMessageAt,
                    unread = c?.UnreadForInstructor ?? 0
                };
            })
            .OrderByDescending(x => x.lastMessageAt ?? DateTime.MinValue)
            .ToList();

            return Json(result);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetChatHistory(string studentId, int take = 10, long? beforeId = null)
        {
            var instructorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(instructorId)) return Unauthorized();
            if (string.IsNullOrWhiteSpace(studentId)) return BadRequest();

            if (take < 1 || take > 50) take = 10;

            // 1) هات Conversation بين الطالب والمدرس
            var convo = await _unitOfWork.ConversationRepository.GetOneAsync(
                c => c.StudentId == studentId && c.InstructorId == instructorId,
                tracked: false
            );

            if (convo == null)
                return Json(new { conversationId = 0, hasMore = false, messages = Array.Empty<object>() });

            // 2) هات آخر رسائل (pagination)
            var messages = (await _unitOfWork.CHMessageRepository.GetAsync(
                m => m.ConversationId == convo.Id && (beforeId == null || m.Id < beforeId),
                tracked: false,
                orderBy: q => q.OrderByDescending(m => m.Id),
                take: take
            )).ToList();

            var list = messages
                .OrderBy(m => m.Id) // عشان تظهر من القديم للجديد
                .Select(m => new
                {
                    id = m.Id,
                    senderId = m.SenderId,
                    receiverId = m.ReceiverId,
                    content = m.Content,
                    sentAt = m.SentAt,
                    isRead = m.IsRead
                })
                .ToList();

            var hasMore = messages.Count == take;

            return Json(new
            {
                conversationId = convo.Id,
                hasMore,
                messages = list
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkAsRead(string studentId)
        {
            var instructorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(instructorId)) return Unauthorized();
            if (string.IsNullOrWhiteSpace(studentId)) return BadRequest();

            var convo = await _unitOfWork.ConversationRepository.GetOneAsync(
                c => c.StudentId == studentId && c.InstructorId == instructorId,
                tracked: true
            );

            if (convo == null) return Ok();

            convo.UnreadForInstructor = 0;

            var msgs = await _unitOfWork.CHMessageRepository.GetAsync(
                m => m.ConversationId == convo.Id
                     && m.ReceiverId == instructorId   // ✅ المهم: الرسائل اللي مستقبلها المدرس
                     && !m.IsRead,
                tracked: true
            );

            foreach (var m in msgs)
            {
                m.IsRead = true;
                m.ReadAt = DateTime.UtcNow;
            }

            await _unitOfWork.CommitAsync();
            return Ok();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveMessage(string receiverId, string content)
        {
            var instructorId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(instructorId))
                return Unauthorized();

            content = content?.Trim();
            if (string.IsNullOrWhiteSpace(receiverId) || string.IsNullOrWhiteSpace(content))
                return Json(new { success = false, message = "Invalid data" });

            var studentId = receiverId;

            // 1) Get conversation (student + instructor)
            var convo = await _unitOfWork.ConversationRepository
                .GetOneAsync(c => c.StudentId == studentId && c.InstructorId == instructorId, tracked: true);

            if (convo == null)
            {
                convo = new Conversation
                {
                    StudentId = studentId,
                    InstructorId = instructorId,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow,
                    LastMessagePreview = "",
                    LastMessageSenderId = instructorId,
                    UnreadForStudent = 0,
                    UnreadForInstructor = 0
                };

                await _unitOfWork.ConversationRepository.CreateAsync(convo);
                await _unitOfWork.CommitAsync(); // عشان convo.Id
            }

            // 2) Save message (sender = instructor)
            var now = DateTime.UtcNow;
            var msg = new CHMessage
            {
                ConversationId = convo.Id,
                SenderId = instructorId,
                ReceiverId = studentId,
                Content = content,
                SentAt = now,
                IsRead = false
            };

            await _unitOfWork.CHMessageRepository.CreateAsync(msg);

            // 3) Update conversation
            convo.LastMessageAt = now;
            convo.LastMessagePreview = content.Length > 50 ? content.Substring(0, 50) : content;
            convo.LastMessageSenderId = instructorId;

            // instructor sent -> student unread +1
            convo.UnreadForStudent += 1;

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






    }
}
