using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;

namespace ELClass.Areas.StudentArea.Controllers
{
    [Area("StudentArea")]
    [Authorize]
    public class LecturesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public LecturesController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Schedule()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var studentAppointments = await _unitOfWork.StudentAppointmentRepository.GetAsync(
                sa => sa.StudentId == userId && sa.Appointment != null,
                q => q
                    .Include(sa => sa.Appointment!)
                        .ThenInclude(a => a.Course)
                    .Include(sa => sa.Appointment!)
                        .ThenInclude(a => a.Instructor!)
                            .ThenInclude(i => i.ApplicationUser)
            );

            var now = DateTime.Now;

            DateTime GetStartDateTime(Appointment a)
            {
                if (a.Type == ScheduleType.OneTime && a.SpecificDate.HasValue)
                    return a.SpecificDate.Value.Date.Add(a.StartTime);

                // Recurring: احسب أقرب occurrence جاية
                var today = DateTime.Today;
                var daysAhead = ((int)a.Day - (int)today.DayOfWeek + 7) % 7;
                var start = today.AddDays(daysAhead).Add(a.StartTime);

                // لو نفس اليوم ووقت المحاضرة عدى، خليه للأسبوع الجاي
                if (start <= now)
                    start = start.AddDays(7);

                return start;
            }

            var list = studentAppointments
                .Select(sa =>
                {
                    var a = sa.Appointment!;
                    var start = GetStartDateTime(a);
                    var end = start.AddHours(a.DurationInHours);

                    var courseName = a.Course?.TitleEn ?? a.Course?.TitleAr ?? "Course";

                    // اسم المدرس الحقيقي
                    var instName =
                        a.Instructor?.NameEn ??
                        a.Instructor?.NameAr ??
                        a.Instructor?.ApplicationUser?.UserName ??
                        a.Instructor?.ApplicationUser?.Email ??
                        "Instructor";

                    return new
                    {
                        StudentAppointmentId = sa.Id,
                        Start = start,
                        End = end,
                        CourseName = courseName,
                        InstructorName = instName,
                        ZoomLink = a.MeetingLink
                    };
                })
                // ✅ فلترة: اخفي أي محاضرة انتهت
                .Where(x => x.End > now)
                .OrderBy(x => x.Start)
                .Select(x => new UpcomingLectureVM
                {
                    Id = x.StudentAppointmentId,   // StudentAppointmentId
                    Date = x.Start,
                    SubjectName = x.CourseName,
                    InstructorName = x.InstructorName,
                    ZoomLink = x.ZoomLink
                })
                .ToList();

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Join(int studentAppointmentId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var sa = await _unitOfWork.StudentAppointmentRepository.GetOneAsync(
                x => x.Id == studentAppointmentId && x.StudentId == userId,
                tracked: false,
                include: q => q.Include(x => x.Appointment)
            );

            if (sa?.Appointment == null) return NotFound();

            if (!sa.IsAccessAllowed) return Forbid();

            var link = sa.Appointment.MeetingLink;
            if (string.IsNullOrWhiteSpace(link)) return BadRequest("Meeting link is missing.");

            return Redirect(link);
        }
    }
}

