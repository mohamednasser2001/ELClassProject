using DataAccess.Repositories.IRepositories;
using ELClass.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using Models.ViewModels.Instructor;
using System.Security.Claims;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ELClass.Areas.Instructor.Controllers
{
    [Authorize(Roles = "Instructor")]
    [Area("Instructor")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<ChatHub> _hub;

        public HomeController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IWebHostEnvironment env, IHubContext<ChatHub> hub)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _env = env;
            _hub = hub;
        }

        public async Task<IActionResult> Index()
        {
            var isArabic = CultureHelper.IsArabic;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. حساب الإحصائيات مع التأكد من الربط الصحيح
            var viewModel = new InstructorIndexDashboardVM
            {
                // إجمالي الطلاب المرتبطين بالمدرس من جدول InstructorStudent
                TotalStudents = await _unitOfWork.InstructorStudentRepository
                                     .CountAsync(isd => isd.InstructorId == userId),

                // عدد الدروس في الكورسات اللي المدرس موجود فيها في جدول InstructorCourse
                ActiveLessons = await _unitOfWork.LessonRepository
                                     .CountAsync(l => l.Course.InstructorCourses.Any(ic => ic.InstructorId == userId)),

                // عدد الكورسات اللي المدرس بيدرسها فعلياً
                TotalCourses = await _unitOfWork.InstructorCourseRepository
                                    .CountAsync(ic => ic.InstructorId == userId),

                TotalInstructors = await _unitOfWork.InstructorRepository.CountAsync(),

                // 2. جلب الكورسات مع الطلاب المسجلين فيها (عشان تظهر في الـ Your Courses Status)
                TopPerformingCourses = (await _unitOfWork.CourseRepository.GetAsync(
                    filter: c => c.InstructorCourses.Any(ic => ic.InstructorId == userId), // الفلتر بالانستراكتور
                    include: q => q.Include(c => c.StudentCourses), // مهم جداً عشان نعد الطلاب
                    take: 5))
                    .Select(c => new CourseProgressVM
                    {
                        CourseName = isArabic ? (string.IsNullOrEmpty(c.TitleAr) ? c.TitleEn : c.TitleAr) : c.TitleEn,
                        EnrolledStudents = c.StudentCourses?.Count() ?? 0, // العد الحقيقي للطلاب في الكورس
                        SuccessRate = 100
                    }).OrderByDescending(c => c.EnrolledStudents).ToList()
            };

            // 3. تجهيز بيانات الرسم البياني (آخر 6 أشهر)
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.Date.AddMonths(-i))
                .Reverse()
                .ToList();

            var chartLabels = last6Months.Select(m => m.ToString("MMM yyyy")).ToList();
            var chartData = new List<int>();

            foreach (var month in last6Months)
            {
                // بنعد الطلاب اللي سجلوا حساباتهم في الشهر ده ومرتبطين بالمدرس ده
                var count = await _unitOfWork.InstructorStudentRepository.CountAsync(isd =>
                    isd.InstructorId == userId &&
                    isd.Student.CreatedDate.Month == month.Month &&
                    isd.Student.CreatedDate.Year == month.Year);

                chartData.Add(count);
            }

            ViewBag.ChartLabels = chartLabels;
            ViewBag.ChartData = chartData;

            // جلب قائمة الطلاب لعرضها في الجدول إذا لزم الأمر
            viewModel.Students = (await _unitOfWork.InstructorStudentRepository.GetAsync(
                        e => e.InstructorId == userId,
                        e => e.Include(x => x.Student).ThenInclude(s => s.ApplicationUser)
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
                    isRead = m.IsRead,

                    AttachmentOriginalName = m.AttachmentOriginalName,
                    AttachmentContentType = m.AttachmentContentType,
                    AttachmentSize = m.AttachmentSize,
                    AttachmentUrl = !string.IsNullOrEmpty(m.AttachmentStoredName)
                    ? Url.Action("DownloadAttachment", "Home", new { area = "Instructor", messageId = m.Id })
                    : null,

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

            var msgs = await _unitOfWork.CHMessageRepository.GetAsync(
                m => m.ConversationId == convo.Id
                     && m.ReceiverId == instructorId
                     && !m.IsRead,
                tracked: true
            );

            if (msgs == null || !msgs.Any())
                return Ok();

            var now = DateTime.UtcNow;
            var messageIds = msgs.Select(m => m.Id).ToList();

            foreach (var m in msgs)
            {
                m.IsRead = true;
                m.ReadAt = now;
            }

            convo.UnreadForInstructor = 0;

            await _unitOfWork.CommitAsync();

            // 🔥 الريل تايم: ابعت للطالب إن رسائله اتقريت
            await _hub.Clients.User(convo.StudentId).SendAsync("MessagesRead", new
            {
                conversationId = convo.Id,
                messageIds = messageIds,
                readerId = instructorId,
                readAt = now
            });

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


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DownloadAttachment(long messageId)
        {
            // 1) هات الرسالة
            var msg = await _unitOfWork.CHMessageRepository
                .GetOneAsync(m => m.Id == messageId, tracked: false);

            if (msg == null) return NotFound();
            if (string.IsNullOrWhiteSpace(msg.AttachmentStoredName)) return NotFound();

            // 2) تأمين: المدرس لازم يكون طرف في الرسالة
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var isParticipant = (msg.SenderId == userId) || (msg.ReceiverId == userId);
            if (!isParticipant) return Forbid();

            // 3) نفس مكان التخزين عندك
            var baseDir = Path.Combine(_env.ContentRootPath, "App_Data", "chat_uploads");
            var fullPath = Path.Combine(baseDir, msg.AttachmentStoredName);

            if (!System.IO.File.Exists(fullPath)) return NotFound();

            // 4) رجّع الملف
            var contentType = string.IsNullOrWhiteSpace(msg.AttachmentContentType)
                ? "application/octet-stream"
                : msg.AttachmentContentType;

            var downloadName = string.IsNullOrWhiteSpace(msg.AttachmentOriginalName)
                ? msg.AttachmentStoredName
                : msg.AttachmentOriginalName;

            // صور + PDF يتفتحوا في المتصفح
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return PhysicalFile(fullPath, contentType);
            }

            // باقي الملفات تحميل
            return PhysicalFile(fullPath, contentType, downloadName);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SendMessageWithFile([FromForm] SendMessageWithFileVM vm)
        {
            var senderId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(senderId)) return Unauthorized();
            if (string.IsNullOrWhiteSpace(vm.ReceiverId)) return BadRequest("ReceiverId is required.");

            string? storedName = null;
            string? originalName = null;
            string? contentType = null;
            long? size = null;

            if (vm.File is not null && vm.File.Length > 0)
            {
                var allowedExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".pdf", ".doc", ".docx" };

                var ext = Path.GetExtension(vm.File.FileName);
                if (string.IsNullOrWhiteSpace(ext) || !allowedExt.Contains(ext))
                    return BadRequest("File type not allowed.");

                const long maxBytes = 10 * 1024 * 1024;
                if (vm.File.Length > maxBytes) return BadRequest("File is too large.");

                originalName = Path.GetFileName(vm.File.FileName);
                contentType = vm.File.ContentType;
                size = vm.File.Length;

                storedName = $"{Guid.NewGuid():N}{ext}";
                var uploadDir = Path.Combine(_env.ContentRootPath, "App_Data", "chat_uploads");
                Directory.CreateDirectory(uploadDir);

                var fullPath = Path.Combine(uploadDir, storedName);
                using var fs = System.IO.File.Create(fullPath);
                await vm.File.CopyToAsync(fs);
            }

            // ✅ المدرس -> الطالب: Conversation فيه InstructorId = senderId و StudentId = receiverId
            var convo = await _unitOfWork.ConversationRepository
                .GetOneAsync(c => c.InstructorId == senderId && c.StudentId == vm.ReceiverId, tracked: true);

            if (convo == null)
            {
                convo = new Conversation
                {
                    InstructorId = senderId,
                    StudentId = vm.ReceiverId
                };
                await _unitOfWork.ConversationRepository.CreateAsync(convo);
                await _unitOfWork.CommitAsync();
            }

            var msg = new CHMessage
            {
                ConversationId = convo.Id,
                SenderId = senderId,
                ReceiverId = vm.ReceiverId,
                Content = string.IsNullOrWhiteSpace(vm.Content) ? null : vm.Content.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false,

                AttachmentOriginalName = originalName,
                AttachmentStoredName = storedName,
                AttachmentContentType = contentType,
                AttachmentSize = size
            };

            await _unitOfWork.CHMessageRepository.CreateAsync(msg);
            await _unitOfWork.CommitAsync();
            // ✅ Update conversation preview/time
            var preview =
                !string.IsNullOrWhiteSpace(msg.Content)
                    ? msg.Content
                    : (!string.IsNullOrWhiteSpace(msg.AttachmentStoredName)
                        ? ((msg.AttachmentContentType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                            ? "📷 Photo"
                            : "📎 File")
                        : "");

            convo.LastMessagePreview = preview;
            convo.LastMessageAt = msg.SentAt;

            // (اختياري) لو عندك unread counters بتتحدث هنا برضه
            // convo.UnreadForStudent += 1;

            await _unitOfWork.CommitAsync();


            string? attachmentUrl = null;
            if (!string.IsNullOrWhiteSpace(storedName))
            {
                attachmentUrl = Url.Action("DownloadAttachment", "Home",
                    new { area = "Instructor", messageId = msg.Id });
            }

            var payload = new ChatSendVM
            {
                Id = msg.Id,
                SenderId = msg.SenderId,
                ReceiverId = msg.ReceiverId,
                Content = msg.Content,
                SentAt = msg.SentAt,
                IsRead = msg.IsRead,

                AttachmentOriginalName = msg.AttachmentOriginalName,
                AttachmentContentType = msg.AttachmentContentType,
                AttachmentSize = msg.AttachmentSize,
                AttachmentUrl = attachmentUrl
            };

            // ابعت للطالب لايف
            await _hub.Clients.User(vm.ReceiverId).SendAsync("ReceiveMessage", payload);

            return Json(payload);
        }






    }
}
