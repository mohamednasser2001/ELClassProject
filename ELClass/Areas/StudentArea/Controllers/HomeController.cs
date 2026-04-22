using System.Threading.Tasks;
using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
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
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<ChatHub> _hub;
        public HomeController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IWebHostEnvironment env, IHubContext<ChatHub> hub)
        {
            this._unitOfWork = unitOfWork;
            this._userManager = userManager;
            _env = env;
            _hub = hub;
        }


        [HttpPost]
        public IActionResult ChangeLanguage(string lang)
        {

            HttpContext.Session.SetString("Language", lang);


            return Redirect(Request.Headers["Referer"].ToString());
        }
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var courses = await _unitOfWork.StudentCourseRepository
                .GetAsync(
                    e => e.StudentId == userId,
                    q => q.Include(e => e.Course).ThenInclude(c => c.Lessons)
                );

            var allStudentAppointments = await _unitOfWork.StudentAppointmentRepository
                .GetAsync(
                    sa => sa.StudentId == userId,
                    q => q.Include(sa => sa.Appointment)
                          .ThenInclude(a => a.Course)
                          .Include(sa => sa.Appointment)
                          .ThenInclude(a => a.Instructor)
                          .ThenInclude(i => i.ApplicationUser)
                );

            var studentLessons = await _unitOfWork.StudentLessonRepository
                .GetAsync(
                    sl => sl.StudentId == userId,
                    q => q.Include(sl => sl.Lesson),
                    tracked: false
                );

            var evaluatedCountByCourseId = studentLessons
                .Where(sl => sl.Lesson != null)
                .GroupBy(sl => sl.Lesson.CourseId)
                .ToDictionary(g => g.Key, g => g.Count());

            var appointmentCountByCourseId = allStudentAppointments
                .Where(sa => sa.Appointment != null)
                .GroupBy(sa => sa.Appointment!.CourseId)
                .ToDictionary(g => g.Key, g => g.Count());

            var coursesVM = courses.Select(sc =>
            {
                var attended = evaluatedCountByCourseId.TryGetValue(sc.CourseId, out var c) ? c : 0;
                var totalAppointments = appointmentCountByCourseId.TryGetValue(sc.CourseId, out var t) ? t : 0;
                return new StudentCoursesVM
                {
                    CourseId = sc.CourseId,
                    CourseTitleEn = sc.Course.TitleEn,
                    CourseTitleAr = sc.Course.TitleAr,
                    AttendedCount = attended,
                    GoalCount = totalAppointments
                };
            })
            .Take(4)
            .ToList();

            var allInstructors = await _unitOfWork.InstructorRepository
                .GetAsync(null, q => q.Include(i => i.ApplicationUser));

            var instructorsVM = allInstructors.Select(i => new InstructorChatVM
            {
                InstructorId = i.Id.ToString(),
                InstructorNameEn = i.NameEn,
                InstructorNameAr = i.NameAr,
                ApplicationUser = i.ApplicationUser
            }).ToList();

            var appUser = await _userManager.GetUserAsync(User);

            var model = new StudentDashboardVM
            {
                Courses = coursesVM,
                Instructors = instructorsVM,
                StudentNameEn = appUser?.NameEN ?? "",
                StudentNameAr = appUser?.NameAR ?? ""
            };

            // ============================================================
            // ✅ LastLessons (آخر 5 Lessons المدرس نشرهم للطالب)
            // ============================================================
            var enrolledCourseIds = courses.Select(x => x.CourseId).Distinct().ToList();
            var nowForLessons = DateTime.Now;

            // ✅ هات المدرس المعيّن للطالب (ممكن يكون null)
            var insLink = await _unitOfWork.InstructorStudentRepository.GetOneAsync(x => x.StudentId == userId);
            string? instructorId = insLink?.InstructorId;

            // ✅ لو مفيش مدرس متعيّن => LastLessons فاضية بدل ما نجيب من كل المدرسين
            if (instructorId == null)
            {
                model.LastLessons = new List<Models.ViewModels.LastLessonVM>();
            }
            else
            {
                // published = LectureDate موجودة + مش مستقبل + عنده أي لينك من الثلاثة
                var publishedLessons = await _unitOfWork.LessonRepository.GetAsync(
                    l => enrolledCourseIds.Contains(l.CourseId)
                         && l.InstructorId == instructorId
                         && l.LectureDate != null
                         && l.LectureDate <= nowForLessons
                         && (
                            !string.IsNullOrWhiteSpace(l.DriveLink)
                            || l.LessonMaterials.Any()
                            || l.LessonAssignments.Any()
                        ),

                    tracked: false,
                    include: q => q
                    .Include(l => l.Course)
                    .Include(l => l.LessonMaterials)
                    .Include(l => l.LessonAssignments),
                    orderBy: q => q.OrderByDescending(l => l.LectureDate)
                );

                model.LastLessons = publishedLessons
                    .Take(5)
                    .Select(l => new Models.ViewModels.LastLessonVM
                    {
                        LessonId = l.Id,
                        LessonTitle = string.IsNullOrWhiteSpace(l.Title) ? $"Lesson #{l.Id}" : l.Title,
                        CourseId = l.CourseId,
                        CourseTitle = l.Course != null
                            ? (!string.IsNullOrWhiteSpace(l.Course.TitleEn) ? l.Course.TitleEn : l.Course.TitleAr)
                            : "Course",

                        DriveLink = l.DriveLink,
                        MaterialUrls = l.LessonMaterials.Select(m => m.FileUrl).ToList(),      
                        AssignmentUrls = l.LessonAssignments.Select(a => a.FileUrl).ToList(),
                        LectureDate = l.LectureDate!,
                    })
                    .ToList();
            }

            // ============================================================
            // Upcoming lectures + Join Now (فلترة اللي انتهى)
            // ============================================================
            var now = DateTime.Now;

           

            var lecturesComputed = allStudentAppointments
                .Where(sa => sa.Appointment != null && !sa.IsAttended)
                .Select(sa =>
                {
                    var start = sa.Appointment.StartDateTime;
                    DateTime? end = null;

                    if (start != null)
                        end = start.AddHours(sa.Appointment!.DurationInHours);

                    return new
                    {
                        SA = sa,
                        Start = start,
                        End = end
                    };
                })
                .Where(x => x.Start != null && x.End != null)
                .Where(x => x.End!.Value > now)
                .OrderBy(x => x.Start)
                .ToList();

            model.UpcomingLectures = lecturesComputed
                .Select(x => new UpcomingLectureVM
                {
                    Id = x.SA.Id,
                    Date = x.Start,
                    SubjectName = x.SA.Appointment!.Course != null
                        ? (!string.IsNullOrWhiteSpace(x.SA.Appointment.Course.TitleEn)
                            ? x.SA.Appointment.Course.TitleEn
                            : x.SA.Appointment.Course.TitleAr)
                        : "Lecture",
                    InstructorName = x.SA.Appointment!.Instructor != null
                        ? (!string.IsNullOrWhiteSpace(x.SA.Appointment.Instructor.NameEn)
                            ? x.SA.Appointment.Instructor.NameEn
                            : x.SA.Appointment.Instructor.NameAr)
                        : "Instructor",
                    ZoomLink = x.SA.Appointment!.MeetingLink ?? "",
                    EndDate = x.End
                })
                .ToList();

            var live = lecturesComputed
                .FirstOrDefault(x => now >= x.Start && now <= x.End!.Value);

            model.NextStudentAppointmentId = live?.SA.Id;
            model.CanJoinNow = live != null;

            model.CompletedLessons = studentLessons.Count();
            model.TotalLessons = courses.Sum(sc => sc.Course.Lessons?.Count ?? 0);
            model.OverallProgress = studentLessons.Any()
                ? (int)Math.Round(studentLessons.Average(sl => sl.Degree))
                : 0;

            var attendedAppts = allStudentAppointments.Count(sa => sa.Appointment != null && sa.IsAttended);
            var totalAppts    = allStudentAppointments.Count(sa => sa.Appointment != null);
            model.AttendedAppointmentsCount = attendedAppts;
            model.TotalAppointmentsCount    = totalAppts;
            model.AttendanceRate = totalAppts > 0
                ? (int)Math.Round(attendedAppts * 100.0 / totalAppts)
                : 0;

            model.CourseGrades = courses.Select(sc =>
            {
                var grades = studentLessons
                    .Where(sl => sl.Lesson != null && sl.Lesson.CourseId == sc.CourseId)
                    .ToList();
                return new CourseGradeVM
                {
                    CourseNameEn  = sc.Course.TitleEn ?? "",
                    CourseNameAr  = sc.Course.TitleAr ?? "",
                    AverageGrade  = grades.Any() ? Math.Round(grades.Average(sl => sl.Degree), 1) : 0
                };
            })
            .Where(cg => cg.AverageGrade > 0)
            .ToList();

            model.NewNotifications = allStudentAppointments.Count(sa =>
                sa.Appointment != null &&
                !sa.IsAttended &&
                sa.Appointment.IsActive == true
            );

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
                isRead = m.IsRead,

                AttachmentOriginalName = m.AttachmentOriginalName,
                AttachmentContentType = m.AttachmentContentType,
                AttachmentSize = m.AttachmentSize,
                AttachmentUrl = m.AttachmentStoredName != null
                    ? Url.Action("DownloadAttachment", "Home", new { area = "StudentArea", messageId = m.Id })
                    : null,

            }).ToArray();

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

            var unreadMessages = await _unitOfWork.CHMessageRepository.GetAsync(
                m => m.ConversationId == convo.Id
                     && m.ReceiverId == studentId
                     && m.IsRead == false,
                tracked: true
            );

            if (unreadMessages == null || !unreadMessages.Any())
                return Ok();

            var now = DateTime.UtcNow;
            var messageIds = unreadMessages.Select(m => m.Id).ToList();

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
                msg.ReadAt = now;
            }

            convo.UnreadForStudent = 0;

            await _unitOfWork.CommitAsync();

            await _hub.Clients.User(convo.InstructorId).SendAsync("MessagesRead", new
            {
                conversationId = convo.Id,
                messageIds = messageIds,
                readerId = studentId,
                readAt = now
            });

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

        [HttpPost]
        [ValidateAntiForgeryToken]
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

            var convo = await _unitOfWork.ConversationRepository
                .GetOneAsync(c => c.StudentId == senderId && c.InstructorId == vm.ReceiverId, tracked: true);

            if (convo == null)
            {
                convo = new Conversation
                {
                    StudentId = senderId,
                    InstructorId = vm.ReceiverId
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
                attachmentUrl = Url.Action("DownloadAttachment", "Home", new { area = "StudentArea", messageId = msg.Id });


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

            await _hub.Clients.User(vm.ReceiverId).SendAsync("ReceiveMessage", payload);
          
            return Json(payload);
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DownloadAttachment(long messageId)
        {
            // 1) هات الرسالة من الداتا بيز
            var msg = await _unitOfWork.CHMessageRepository
                .GetOneAsync(m => m.Id == messageId, tracked: false);

            if (msg == null) return NotFound();
            if (string.IsNullOrWhiteSpace(msg.AttachmentStoredName)) return NotFound();

            // 2) (اختياري مهم) تأمين: ما تسمحش لأي حد ينزل ملف مش بتاعه
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var isParticipant = (msg.SenderId == userId) || (msg.ReceiverId == userId);
            if (!isParticipant) return Forbid();

            // 3) مسار التخزين عندك: App_Data/chat_uploads
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

            // صور + PDF يتفتحوا عادي في المتصفح (inline)
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return PhysicalFile(fullPath, contentType);
            }

            // باقي الملفات (Word وغيره) تحميل
            return PhysicalFile(fullPath, contentType, downloadName);
        }







    }
}
