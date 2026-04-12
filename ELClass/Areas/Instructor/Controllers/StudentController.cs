using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using ELClass.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using System.Linq.Expressions;
using System.Security.Claims;

namespace ELClass.Areas.Instructor.Controllers
{
    [Area("Instructor")]
    [Authorize(Roles = "Instructor")]
    public class StudentController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetStudents()
        {
            try
            {
                var isArabic = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ar";
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var draw = Request.Form["draw"].FirstOrDefault();
                var start = int.Parse(Request.Form["start"].FirstOrDefault() ?? "0");
                var length = int.Parse(Request.Form["length"].FirstOrDefault() ?? "10");
                var searchValue = Request.Form["search[value]"].FirstOrDefault() ?? "";
                var orderColumnIndex = Request.Form["order[0][column]"].FirstOrDefault();
                var orderDir = Request.Form["order[0][dir]"].FirstOrDefault() ?? "desc"; ;
                var lang = isArabic ? "ar" : "en";


                Expression<Func<InstructorStudent, bool>> filter = x =>
                    x.InstructorId == userId &&
                    (string.IsNullOrEmpty(searchValue) ||
                     x.Student.NameEn.Contains(searchValue) ||
                     x.Student.NameAr.Contains(searchValue));


                Func<IQueryable<InstructorStudent>, IOrderedQueryable<InstructorStudent>> orderBy = q =>
                {
                    if (orderDir == "asc")
                    {
                        return orderColumnIndex switch
                        {
                            "1" => q.OrderBy(x => lang == "en" ? x.Student.NameEn : x.Student.NameAr),

                            "2" => q.OrderBy(x => x.TimesCount),
                            _ => q.OrderBy(x => x.StudentId)
                        };
                    }


                    return orderColumnIndex switch
                    {
                        "1" => q.OrderByDescending(x => lang == "en" ? x.Student.NameEn : x.Student.NameAr),

                        _ => q.OrderByDescending(x => x.StudentId)
                    };
                };


                var instructorStudents = await _unitOfWork.InstructorStudentRepository.GetAsync(
                    filter: filter,
                    orderBy: orderBy,
                    skip: start,
                    take: length,
                    include: q => q.Include(x => x.Student)
                );

                var totalRecords = await _unitOfWork.InstructorStudentRepository.CountAsync(x => x.InstructorId == userId);
                var filteredRecords = await _unitOfWork.InstructorStudentRepository.CountAsync(filter: filter);

                var data = instructorStudents.Select(x => new
                {
                    id = x.Student.Id,
                    name = lang == "en" ? x.Student.NameEn : x.Student.NameAr,
                    timesCount = x.TimesCount
                }).ToList();

                return Json(new { draw, recordsTotal = totalRecords, recordsFiltered = filteredRecords, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { draw = Request.Form["draw"].FirstOrDefault(), recordsTotal = 0, recordsFiltered = 0, data = new List<object>(), error = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetSharedCourses(string studentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isArabic = CultureHelper.IsArabic;

            // الكورسات اللي المدرس بيدرسها
            var instructorCourses = await _unitOfWork.InstructorCourseRepository
                .GetAsync(ic => ic.InstructorId == userId, include: ic => ic.Include(x => x.Course));

            var instructorCourseIds = instructorCourses.Select(ic => ic.CourseId).ToList();

            // الكورسات اللي الطالب مسجل فيها من نفس الكورسات
            var studentCourses = await _unitOfWork.StudentCourseRepository
                .GetAsync(sc => sc.StudentId == studentId && instructorCourseIds.Contains(sc.CourseId),
                          include: sc => sc.Include(x => x.Course));

            var result = studentCourses.Select(sc => new
            {
                id = sc.CourseId,
                text = isArabic ? sc.Course.TitleAr : sc.Course.TitleEn
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> SearchStudentsForAppointment(string term, int courseId, string excludeStudentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isArabic = CultureHelper.IsArabic;

            // طلاب مسجلين مع المدرس ده
            var instructorStudents = await _unitOfWork.InstructorStudentRepository
                .GetAsync(x => x.InstructorId == userId, include: x => x.Include(s => s.Student));

            var instructorStudentIds = instructorStudents.Select(x => x.StudentId).ToList();

            // طلاب مسجلين في الكورس ده
            var courseStudents = await _unitOfWork.StudentCourseRepository
                .GetAsync(sc => sc.CourseId == courseId &&
                                sc.StudentId != excludeStudentId &&
                                instructorStudentIds.Contains(sc.StudentId) &&
                                (string.IsNullOrEmpty(term) ||
                                 sc.Student.NameEn.Contains(term) ||
                                 sc.Student.NameAr.Contains(term)),
                          include: sc => sc.Include(x => x.Student));

            var result = courseStudents.Select(sc => new
            {
                id = sc.StudentId,
                text = isArabic
                    ? $"{sc.Student.NameAr} - {sc.Student.NameEn}"
                    : $"{sc.Student.NameEn} - {sc.Student.NameAr}"
            }).ToList();

            return Json(result);
        }



        [HttpGet]
        public async Task<IActionResult> GetStudentTimesCount(string studentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var instructorStudent = await _unitOfWork.InstructorStudentRepository
                .GetOneAsync(x => x.InstructorId == userId && x.StudentId == studentId);
            return Json(new { timesCount = instructorStudent?.TimesCount ?? 0 });
        }

        [HttpPost]
        public async Task<IActionResult> CreateBulk([FromBody] BulkAppointmentVM vm)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isArabic = CultureHelper.IsArabic;

                if (vm.Appointments == null || !vm.Appointments.Any())
                    return Json(new { success = false, message = isArabic ? "أضف ميعاد واحد على الأقل" : "Add at least one appointment" });

                // التحقق من التعارض
                var existingAppointments = await _unitOfWork.AppoinmentRepository
                    .GetAsync(a => a.InstructorId == userId);

                foreach (var slot in vm.Appointments)
                {
                    slot.DurationInHours = 1;
                    var slotEnd = slot.StartDateTime.AddHours(slot.DurationInHours);
                    var conflict = existingAppointments.FirstOrDefault(a =>
                        slot.StartDateTime < a.EndDateTime && slotEnd > a.StartDateTime);

                    if (conflict != null)
                        return Json(new
                        {
                            success = false,
                            message = (isArabic ? "يوجد تعارض في: " : "Conflict at: ")
                                      + conflict.StartDateTime.ToString("dd/MM/yyyy hh:mm tt")
                        });
                }

     
                foreach (var slot in vm.Appointments)
                {
                    var appointment = new Appointment
                    {
                        InstructorId = userId,
                        CourseId = vm.CourseId,
                        StartDateTime = slot.StartDateTime,
                        DurationInHours = slot.DurationInHours,
                        MeetingLink = slot.MeetingLink
                    };
                    await _unitOfWork.AppoinmentRepository.CreateAsync(appointment);
                    await _unitOfWork.CommitAsync(); // علشان نجيب الـ Id

                    // ربط كل الطلاب بالميعاد
                    foreach (var studentId in vm.StudentIds)
                    {
                        await _unitOfWork.StudentAppointmentRepository.CreateAsync(new StudentAppointment
                        {
                            AppointmentId = appointment.Id,
                            StudentId = studentId
                        });

                        // تحديث TimesCount لكل طالب
                        var instructorStudent = await _unitOfWork.InstructorStudentRepository
                            .GetOneAsync(x => x.InstructorId == userId && x.StudentId == studentId);
                        if (instructorStudent != null)
                            instructorStudent.TimesCount = Math.Max(0, instructorStudent.TimesCount - 1);
                    }
                }

                await _unitOfWork.CommitAsync();

                return Json(new
                {
                    success = true,
                    message = isArabic
                        ? $"تم إضافة {vm.Appointments.Count} مواعيد بنجاح"
                        : $"{vm.Appointments.Count} appointments added successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }


    }
}
