using DataAccess.Repositories.IRepositories;
using ELClass.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using System.Security.Claims;

namespace ELClass.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AppointmentController(IUnitOfWork _unitOfWork) : Controller
    {
        private readonly IUnitOfWork unitOfWork = _unitOfWork;

        public async Task<IActionResult> Index()
        {

            var appointments = (await unitOfWork.AppoinmentRepository.GetAsync(
                include: e => e.Include(e => e.Course!)
                              .Include(e => e.StudentAppointments)
                              .Include(e => e.Instructor!)
            )).ToList();


            var expiredApps = appointments
                .Where(app => app.Type == ScheduleType.OneTime &&
                              app.SpecificDate.HasValue &&
                              app.SpecificDate.Value.Date < DateTime.Now.Date)
                .ToList();

            if (expiredApps.Any())
            {

                var studentAppsToDelete = expiredApps
                    .SelectMany(a => a.StudentAppointments)
                    .ToList();

                if (studentAppsToDelete.Any())
                {
                    await unitOfWork.StudentAppointmentRepository.DeleteAllAsync(studentAppsToDelete);
                }


                await unitOfWork.AppoinmentRepository.DeleteAllAsync(expiredApps);

                await unitOfWork.CommitAsync();


                appointments = appointments.Except(expiredApps).ToList();
            }

            return View(appointments);
        }


        public async Task<IActionResult> ManageStudents(int id)
        {

            var appointment = await unitOfWork.AppoinmentRepository.GetOneAsync(
                a => a.Id == id,
                include: e => e.Include(e => e.Course).Include(e => e.StudentAppointments).ThenInclude(e => e.Student!));

            if (appointment == null) return NotFound();

            return View(appointment);
        }

        public async Task<IActionResult> SearchStudents(string term, int? courseId = null, int? appointmentId = null)
        {
            // 1. تحديد الـ InstructorId لو فيه موعد محدد
            string? insId = null;
            if (appointmentId.HasValue)
            {
                var appointment = await unitOfWork.AppoinmentRepository.GetOneAsync(a => a.Id == appointmentId);
                insId = appointment?.InstructorId;
            }

            // 2. الفلترة الأساسية (تتم داخل قاعدة البيانات لتحسين الأداء)
            var studentCourses = await unitOfWork.StudentCourseRepository.GetAsync(
                filter: sc =>
                    // فلتر الكورس لو موجود
                    (!courseId.HasValue || sc.CourseId == courseId) &&
                    // فلتر المحاضر لو موجود
                    (string.IsNullOrEmpty(insId) || sc.Student.InstructorStudents.Any(isc => isc.InstructorId == insId)) &&
                    // فلتر نص البحث (الاسم أو الإيميل)
                    (string.IsNullOrEmpty(term) ||
                     sc.Student.NameEn.Contains(term) ||
                     sc.Student.NameAr.Contains(term) ||
                     sc.Student.ApplicationUser.Email!.Contains(term)),
                include: q => q.Include(sc => sc.Student)
                               .ThenInclude(s => s.ApplicationUser)
                               .Include(sc => sc.Student)
                               .ThenInclude(s => s.InstructorStudents)
            );

            // 3. تحويل النتائج للشكل المطلوب لـ Select2
            var results = studentCourses.Select(sc => new
            {
                id = sc.StudentId,
                text = CultureHelper.IsArabic
                        ? $"{sc.Student.NameAr} - {sc.Student.NameEn}"
                        : $"{sc.Student.NameEn} - {sc.Student.NameAr}"
            }).DistinctBy(x => x.id).ToList(); // Distinct عشان الطالب ميتكررش لو مشترك في كذا كورس

            return Json(results);
        }

        [HttpPost]
        public async Task<IActionResult> AddStudentToAppointment(string StudentId, int AppointmentId, int TimeCount)
        {

            var isExists = await unitOfWork.StudentAppointmentRepository
                .GetOneAsync(e => e.StudentId == StudentId && e.AppointmentId == AppointmentId);

            if (isExists != null)
            {

                var errorMsg = CultureHelper.IsArabic
                    ? "هذا الطالب مضاف بالفعل لهذا الموعد."
                    : "This student is already added to this appointment.";

                return Json(new { success = false, message = errorMsg });
            }


            var appointment = await unitOfWork.AppoinmentRepository.GetOneAsync(e => e.Id == AppointmentId);
            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment not found." });
            }
            if (appointment.Type == ScheduleType.OneTime && appointment.SpecificDate.HasValue)
            {
                if (appointment.SpecificDate.Value.Date < DateTime.Now.Date)
                {
                    return Json(new { success = false, message = "لا يمكن إضافة طلاب لموعد قديم" });
                }
            }
            DateTime? expiryDate = null;

            if (appointment.Type == ScheduleType.Recurring)
                expiryDate = DateTime.Now.AddDays(TimeCount * 7);
            else
                expiryDate = appointment.SpecificDate;

            var link = new StudentAppointment
            {
                StudentId = StudentId,
                AppointmentId = AppointmentId,
                TimeCount = TimeCount,
                StudentExpiryDate = expiryDate
            };


            await unitOfWork.StudentAppointmentRepository.CreateAsync(link);
            await unitOfWork.CommitAsync();
            TempData["Success"] = CultureHelper.IsArabic ? "تم اضافة الطالب بنجاح " : " the student has been added successfully";
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveStudentFromAppointment(int studentAppointmentId)
        {
            var item = await unitOfWork.StudentAppointmentRepository.GetOneAsync(e => e.Id == studentAppointmentId);
            if (item != null)
            {
                await unitOfWork.StudentAppointmentRepository.DeleteAsync(item);
                await unitOfWork.CommitAsync();

                return Json(new { success = true });
            }

            return Json(new { success = false });
        }


        [HttpGet]
        public async Task<IActionResult> SearchInstructors(string term)
        {

            var instructors = await unitOfWork.InstructorRepository.GetAsync(
                filter: i => string.IsNullOrEmpty(term) ||
                             i.NameEn.Contains(term) ||
                             i.NameAr.Contains(term) ||
                             i.ApplicationUser.Email.Contains(term),
                include: q => q.Include(i => i.ApplicationUser)
            );

            var results = instructors.Select(i => new
            {
                id = i.Id,
                text = CultureHelper.IsArabic
                        ? $"{i.NameAr} ({i.ApplicationUser.Email})"
                        : $"{i.NameEn} ({i.ApplicationUser.Email})"
            }).ToList();

            return Json(results);
        }

        public IActionResult AddNewAppointment()
        {
            return View();
        }

        public async Task<JsonResult> SearchCourses(string term)
        {



            var courses = await unitOfWork.InstructorCourseRepository.GetAsync(
                filter: ic =>
                         (string.IsNullOrEmpty(term) ||
                          ic.Course.TitleAr.Contains(term) ||
                          ic.Course.TitleEn.Contains(term)), include: ic => ic.Include(ic => ic.Course)
            );

            var results = courses.Select(c => new
            {
                id = c.CourseId,
                text = CultureHelper.IsArabic
                       ? c.Course.TitleAr : c.Course.TitleEn
            }).ToList();

            return Json(results);
        }

        [HttpPost]
        public async Task<IActionResult> Create(AppointmentVM vm)
        {



            try
            {
                var isAuthorized = await unitOfWork.InstructorCourseRepository.GetOneAsync(ic =>
                ic.InstructorId == vm.InstructorId && ic.CourseId == vm.CourseId);

                if (isAuthorized == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = CultureHelper.IsArabic
                            ? "هذا المحاضر غير مسجل لتدريس هذا الكورس"
                            : "This instructor is not assigned to this course"
                    });
                }

                if (vm.Type == (int)ScheduleType.OneTime)
                {
                    if (!vm.SpecificDate.HasValue)
                    {
                        return Json(new { success = false, message = CultureHelper.IsArabic ? "يجب تحديد التاريخ" : "Date is required" });
                    }

                    if (vm.SpecificDate.Value.Date < DateTime.Today)
                    {
                        return Json(new { success = false, message = CultureHelper.IsArabic ? "لا يمكن إضافة موعد بتاريخ قديم" : "Cannot create appointment with a past date" });
                    }
                }

                var newStart = vm.StartTime;
                var newEnd = vm.StartTime.Add(TimeSpan.FromHours(vm.DurationInHours));

                var instructorDayAppointments = await unitOfWork.AppoinmentRepository.GetAsync(a =>
                    a.InstructorId == vm.InstructorId &&
                    (vm.Type == (int)ScheduleType.Recurring ? a.Day == vm.Day : a.SpecificDate == vm.SpecificDate)
                );


                var overlappingAppointment = instructorDayAppointments.FirstOrDefault(a =>
                    newStart < a.StartTime.Add(TimeSpan.FromHours(a.DurationInHours)) &&
                    newEnd > a.StartTime
                );

                if (overlappingAppointment != null)
                {
                    var displayTime = DateTime.Today.Add(overlappingAppointment.StartTime).ToString("hh:mm tt");
                    var msg = CultureHelper.IsArabic
                        ? $"يوجد تعارض مع موعد يبدأ الساعة {displayTime}"
                        : $"Conflict with an appointment starting at {displayTime}";

                    return Json(new { success = false, message = msg });
                }


                var appointment = new Appointment
                {
                    InstructorId = vm.InstructorId,
                    MeetingLink = vm.MeetingLink,
                    Type = (ScheduleType)vm.Type,
                    DurationInHours = vm.DurationInHours,
                    StartTime = vm.StartTime,
                    CourseId = vm.CourseId,
                    Day = vm.Type == (int)ScheduleType.Recurring ? vm.Day : 0,
                    SpecificDate = vm.Type == (int)ScheduleType.OneTime ? vm.SpecificDate : null
                };

                await unitOfWork.AppoinmentRepository.CreateAsync(appointment);
                await unitOfWork.CommitAsync();

                return Json(new { success = true, message = CultureHelper.IsArabic ? "تم إنشاء الموعد بنجاح" : "Appointment created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var appointment = await unitOfWork.AppoinmentRepository.GetOneAsync(e => e.Id == id
                , include: e => e.Include(e => e.Course!).Include(e => e.Instructor!));
            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Models.Appointment model)
        {
            try
            {

                var existing = await unitOfWork.AppoinmentRepository.GetOneAsync(e => e.Id == model.Id);
                if (existing == null)
                {
                    return Json(new { success = false, message = CultureHelper.IsArabic ? "الموعد غير موجود" : "Appointment not found" });
                }


                int targetDayInt = (int)model.Day;
                var newStart = model.StartTime;
                var newEnd = model.StartTime.Add(TimeSpan.FromHours(model.DurationInHours));


                var potentialConflicts = await unitOfWork.AppoinmentRepository.GetAsync(a =>
                    a.Id != model.Id &&
                    a.InstructorId == existing.InstructorId &&
                    (
                        (a.Type == ScheduleType.Recurring && (int)a.Day == targetDayInt) ||
                        (a.Type == ScheduleType.OneTime)
                    )
                );


                var overlapping = potentialConflicts.FirstOrDefault(a =>
                {
                    bool isSameDay = false;

                    if (model.Type == ScheduleType.Recurring)
                    {

                        if (a.Type == ScheduleType.Recurring && a.Day == model.Day) isSameDay = true;
                        else if (a.Type == ScheduleType.OneTime && a.SpecificDate.Value.DayOfWeek == (DayOfWeek)model.Day) isSameDay = true;
                    }
                    else 
                    {

                        if (a.Type == ScheduleType.Recurring && a.Day == (DayOfWeek)model.SpecificDate.Value.DayOfWeek) isSameDay = true;
                        else if (a.Type == ScheduleType.OneTime && a.SpecificDate.Value.Date == model.SpecificDate.Value.Date) isSameDay = true;
                    }

                    if (!isSameDay) return false;

                
                    var existingStart = a.StartTime;
                    var existingEnd = a.StartTime.Add(TimeSpan.FromHours(a.DurationInHours));

                    return newStart < existingEnd && newEnd > existingStart;
                });

                if (overlapping != null)
                {
                    var displayTime = DateTime.Today.Add(overlapping.StartTime).ToString("hh:mm tt");
                    var msg = CultureHelper.IsArabic
                        ? $"يوجد تعارض مع موعد آخر يبدأ الساعة {displayTime}"
                        : $"Conflict with another appointment starting at {displayTime}";

                    return Json(new { success = false, message = msg });
                }


                existing.Type = model.Type;
                existing.StartTime = model.StartTime;
                existing.DurationInHours = model.DurationInHours;
                existing.MeetingLink = model.MeetingLink;

                if (model.Type == ScheduleType.Recurring)
                {
                    existing.Day = model.Day;
                    existing.SpecificDate = null; 
                }
                else
                {
                    existing.SpecificDate = model.SpecificDate;
                    existing.Day = 0; 
                }


                await unitOfWork.AppoinmentRepository.EditAsync(existing);
                await unitOfWork.CommitAsync();

                return Json(new { success = true, message = CultureHelper.IsArabic ? "تم تعديل الموعد بنجاح" : "Appointment updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {

                var appointment = await unitOfWork.AppoinmentRepository.GetOneAsync(e => e.Id == id);
                if (appointment == null)
                {
                    return Json(new { success = false, message = "Appointment not found" });
                }


                var studentAppointments = await unitOfWork.StudentAppointmentRepository.GetAsync(filter: x => x.AppointmentId == id);
                foreach (var sa in studentAppointments)
                {
                    await unitOfWork.StudentAppointmentRepository.DeleteAsync(sa);
                }


                await unitOfWork.AppoinmentRepository.DeleteAsync(appointment);


                await unitOfWork.CommitAsync();

                return Json(new { success = true, message = "Deleted Successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

    }
}
