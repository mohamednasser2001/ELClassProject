using DataAccess.Repositories.IRepositories;
using ELClass.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ELClass.Areas.Instructor.Controllers
{
    [Area("Instructor")]
    [Authorize(Roles = "Instructor")]
    public class AppointmentController(IUnitOfWork unitOfWork) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<IActionResult> Index()
        {

            var userId = User.Claims.FirstOrDefault(c => c.Type.EndsWith("nameidentifier"))?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(); 
            }

            
            var appointments = (await _unitOfWork.AppoinmentRepository.GetAsync(
                filter: e => e.InstructorId == userId,
                include: e => e.Include(e => e.Course!)
                              .Include(e => e.StudentAppointments)
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
                    await _unitOfWork.StudentAppointmentRepository.DeleteAllAsync(studentAppsToDelete);
                }

                
                await _unitOfWork.AppoinmentRepository.DeleteAllAsync(expiredApps);

                
                await _unitOfWork.CommitAsync();

               
                appointments = appointments.Except(expiredApps).ToList();
            }

            return View(appointments);
        }

        public IActionResult Create(string StudentId)
        {
            ViewBag.StdId = StudentId;

            return View();
        }




        [HttpPost]
        public async Task<IActionResult> Create(AppointmentVM vm)
        {
            try
            {

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

                var instructorDayAppointments = await _unitOfWork.AppoinmentRepository.GetAsync(a =>
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

                await _unitOfWork.AppoinmentRepository.CreateAsync(appointment);
                await _unitOfWork.CommitAsync();

                return Json(new { success = true, message = CultureHelper.IsArabic ? "تم إنشاء الموعد بنجاح" : "Appointment created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchStudents(string term, int courseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            
            var studentCourses = await _unitOfWork.StudentCourseRepository.GetAsync(
                filter: sc => sc.CourseId == courseId &&
                              (string.IsNullOrEmpty(term) ||
                               sc.Student.NameEn.Contains(term) ||
                               sc.Student.NameAr.Contains(term ) ||
                               sc.Student.ApplicationUser.Email!.Contains(term)),
                include: q => q.Include(sc => sc.Student)
                               .ThenInclude(s => s.InstructorStudents) 
            );

           
            var results = studentCourses
                .Where(sc => sc.Student.InstructorStudents.Any(isc => isc.InstructorId == userId))
                .Select(sc => new
                {
                    id = sc.StudentId,
                    text = CultureHelper.IsArabic
                           ? $"{sc.Student.NameAr} - {sc.Student.NameEn}"
                           : $"{sc.Student.NameEn} - {sc.Student.NameAr}"
                }).ToList();

            return Json(results);
        }

        public async Task<JsonResult> SearchCourses(string term)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            
            var courses = await _unitOfWork.InstructorCourseRepository.GetAsync(
                filter: ic => ic.InstructorId == userId &&
                         (string.IsNullOrEmpty(term) ||
                          ic.Course.TitleAr.Contains(term) ||
                          ic.Course.TitleEn.Contains(term)) , include: ic => ic.Include(ic => ic.Course)
            );

            var results = courses.Select(c => new
            {
                id = c.CourseId,
                text = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ar"
                       ? c.Course.TitleAr : c.Course.TitleEn
            }).ToList();

            return Json(results);
        }


        public async Task<IActionResult> SearchInstructors(string term, int courseId)
        {
            var instructors = await _unitOfWork.InstructorRepository.GetAsync(filter: e =>
            (e.NameAr.Contains(term) || e.NameEn.Contains(term) || e.ApplicationUser.Email!.Contains(term)));


            var insCourse = await _unitOfWork.InstructorCourseRepository.GetAsync(filter: e => e.CourseId == courseId);
            if (insCourse.Any())
            {
                var assignedStudentIds = insCourse.Select(ic => ic.InstructorId).ToList();
                instructors = instructors.Where(c => !assignedStudentIds.Contains(c.Id)).ToList();
            }

            var result = instructors
                .Select(c => new
                {
                    id = c.Id,
                    text = $"{c.NameEn} - {c.NameAr}"
                });

            return Json(result);
        }


        public IActionResult AddNewAppointment()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {

                var appointment = await _unitOfWork.AppoinmentRepository.GetOneAsync(e => e.Id == id);
                if (appointment == null)
                {
                    return Json(new { success = false, message = "Appointment not found" });
                }


                var studentAppointments = await _unitOfWork.StudentAppointmentRepository.GetAsync(filter: x => x.AppointmentId == id);
                foreach (var sa in studentAppointments)
                {
                    await _unitOfWork.StudentAppointmentRepository.DeleteAsync(sa);
                }


                await _unitOfWork.AppoinmentRepository.DeleteAsync(appointment);


                await _unitOfWork.CommitAsync();

                return Json(new { success = true, message = "Deleted Successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var appointment = await _unitOfWork.AppoinmentRepository.GetOneAsync(e => e.Id == id
                , include: e => e.Include(e => e.Course!));
            return View(appointment);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Models.Appointment model)
        {
            try
            {

                var existing = await _unitOfWork.AppoinmentRepository.GetOneAsync(e => e.Id == model.Id);
                if (existing == null)
                {
                    return Json(new { success = false, message = CultureHelper.IsArabic ? "الموعد غير موجود" : "Appointment not found" });
                }


                int targetDayInt = (int)model.Day;
                var newStart = model.StartTime;
                var newEnd = model.StartTime.Add(TimeSpan.FromHours(model.DurationInHours));


                var potentialConflicts = await _unitOfWork.AppoinmentRepository.GetAsync(a =>
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


                await _unitOfWork.AppoinmentRepository.EditAsync(existing);
                await _unitOfWork.CommitAsync();

                return Json(new { success = true, message = CultureHelper.IsArabic ? "تم تعديل الموعد بنجاح" : "Appointment updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }


        public async Task<IActionResult> ManageStudents(int id)
        {

            var appointment = await _unitOfWork.AppoinmentRepository.GetOneAsync(
                a => a.Id == id,
                include: e => e.Include(e => e.Course).Include(e => e.StudentAppointments).ThenInclude(e => e.Student!));

            if (appointment == null) return NotFound();

            return View(appointment);
        }


        [HttpPost]
        public async Task<IActionResult> AddStudentToAppointment(string StudentId, int AppointmentId, int TimeCount)
        {
            
            var isExists = await _unitOfWork.StudentAppointmentRepository
                .GetOneAsync(e => e.StudentId == StudentId && e.AppointmentId == AppointmentId);

            if (isExists != null)
            {
               
                var errorMsg = CultureHelper.IsArabic
                    ? "هذا الطالب مضاف بالفعل لهذا الموعد."
                    : "This student is already added to this appointment.";

                return Json(new { success = false, message = errorMsg });
            }

            
            var appointment = await _unitOfWork.AppoinmentRepository.GetOneAsync(e => e.Id == AppointmentId);
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

            
            await _unitOfWork.StudentAppointmentRepository.CreateAsync(link);
            await _unitOfWork.CommitAsync();
            TempData["Success"] = CultureHelper.IsArabic ? "تم اضافة الطالب بنجاح " : " the student has been added successfully";
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveStudentFromAppointment(int studentAppointmentId)
        {
            var item = await _unitOfWork.StudentAppointmentRepository.GetOneAsync(e=>e.Id == studentAppointmentId);
            if (item != null)
            {
                await _unitOfWork.StudentAppointmentRepository.DeleteAsync(item);
                await _unitOfWork.CommitAsync();
                
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }


    }
}
