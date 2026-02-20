using System.Security.Claims;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels.Student;

public class StudentNotificationsViewComponent : ViewComponent
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentNotificationsViewComponent(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync(bool isArabic)
    {
        var userId = _userManager.GetUserId(HttpContext.User);

        if (string.IsNullOrEmpty(userId))
            return View(new List<AppointmentNotifyVM>());

        var now = DateTime.Now;

        // ✅ نفس Include بتاع الـ Index عندك
        var allStudentAppointments = await _unitOfWork.StudentAppointmentRepository
            .GetAsync(
                sa => sa.StudentId == userId,
                q => q.Include(sa => sa.Appointment)
                      .ThenInclude(a => a.Course)
                      .Include(sa => sa.Appointment)
                      .ThenInclude(a => a.Instructor)
                      .ThenInclude(i => i.ApplicationUser)
            );

        DateTime? GetStartDateTime(Appointment appt)
        {
            if (appt.Type == ScheduleType.OneTime)
            {
                if (!appt.SpecificDate.HasValue) return null;
                return appt.SpecificDate.Value.Date + appt.StartTime;
            }

            var daysAhead = ((int)appt.Day - (int)now.DayOfWeek + 7) % 7;
            var candidate = now.Date.AddDays(daysAhead) + appt.StartTime;

            if (daysAhead == 0 && candidate <= now)
                candidate = candidate.AddDays(7);

            return candidate;
        }

        // ✅ نفس منطق upcoming + فلترة اللي انتهى
        var lecturesComputed = allStudentAppointments
            .Where(sa => sa.Appointment != null && !sa.IsAttended)
            .Select(sa =>
            {
                var start = GetStartDateTime(sa.Appointment!);
                DateTime? end = null;

                if (start != null)
                    end = start.Value.AddHours(sa.Appointment!.DurationInHours);

                return new { SA = sa, Start = start, End = end };
            })
            .Where(x => x.Start != null && x.End != null)
            .Where(x => x.End!.Value > now)
            .OrderBy(x => x.Start)
            .Take(5)
            .ToList();

        var items = lecturesComputed.Select(x =>
        {
            var appt = x.SA.Appointment!;
            var courseName = appt.Course != null
                ? (!string.IsNullOrWhiteSpace(appt.Course.TitleEn) ? appt.Course.TitleEn : appt.Course.TitleAr)
                : (isArabic ? "محاضرة" : "Lecture");

            var instructorName = appt.Instructor != null
                ? (!string.IsNullOrWhiteSpace(appt.Instructor.NameEn) ? appt.Instructor.NameEn : appt.Instructor.NameAr)
                : (isArabic ? "مدرس" : "Instructor");

            return new AppointmentNotifyVM
            {
                AppointmentId = appt.Id,
                CourseName = courseName,
                InstructorName = instructorName,
                MeetingLink = appt.MeetingLink ?? "",
                StartsAt = x.Start!.Value,
                DisplayText = isArabic
                    ? $"{x.Start!.Value:ddd, dd MMM} • {x.Start!.Value:hh:mm tt}"
                    : $"{x.Start!.Value:ddd, dd MMM} • {x.Start!.Value:hh:mm tt}"
            };
        }).ToList();

        ViewBag.NotifyCount = items.Count; // لو عايز العدد الكلي مش الـ 5 قولّي
        ViewBag.IsArabic = isArabic;

        return View(items);
    }
}
