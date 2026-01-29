using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ELClass.Areas.Instructor.Controllers
{
    [Area("Instructor")]
    public class AppointmentController(IUnitOfWork unitOfWork) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<IActionResult> Index()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type.EndsWith("nameidentifier"))?.Value;
            var appointments = await _unitOfWork.AppoinmentRepository.GetAsync(e => e.InstructorId == userId, include: e => e.Include(e => e.Course!).Include(e=>e.StudentAppointments));
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
                // 1. حساب تاريخ انتهاء الطالب (Student Expiry Date)
                DateTime? calculatedStudentExpiryDate = null;

                if (vm.Type == (int)ScheduleType.Recurring)
                {
                    DateTime startDate = DateTime.Today;
                    while (startDate.DayOfWeek != vm.Day)
                    {
                        startDate = startDate.AddDays(1);
                    }

                    if (vm.TimeCount > 0)
                    {
                        calculatedStudentExpiryDate = startDate.AddDays((vm.TimeCount - 1) * 7);
                    }
                }
                else // OneTime
                {
                    calculatedStudentExpiryDate = vm.SpecificDate;
                }

                // 2. البحث عن موعد موجود مسبقاً بنفس المواصفات للمدرس ده
                // بندور على (نفس المدرس، نفس الكورس، نفس اليوم، نفس وقت البدء، نفس النوع)
                var existingAppointment = await _unitOfWork.AppoinmentRepository.GetOneAsync(a =>
                    a.InstructorId == vm.InstructorId &&
                    a.CourseId == vm.CourseId &&
                    a.Type == (ScheduleType)vm.Type &&
                    a.StartTime == vm.StartTime &&
                    (vm.Type == (int)ScheduleType.Recurring ? a.Day == vm.Day : a.SpecificDate == vm.SpecificDate)
                );

                int appointmentId;

                if (existingAppointment != null)
                {
                    // لو الموعد موجود، هنستخدم الـ ID بتاعه
                    appointmentId = existingAppointment.Id;
                }
                else
                {
                    // لو مش موجود، هننشئ موعد جديد (الماستر)
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
                    appointmentId = appointment.Id;
                }

                // 3. التحقق إذا كان الطالب مسجل في هذا الموعد مسبقاً (عشان منكررهوش)
                var existingStudentLink = await _unitOfWork.StudentAppointmentRepository.GetOneAsync(sa =>
                    sa.AppointmentId == appointmentId && sa.StudentId == vm.StudentId);

                if (existingStudentLink != null)
                {
                    // لو الطالب موجود أصلاً، ممكن نحدث تاريخ انتهائه فقط (تجديد اشتراك)
                    existingStudentLink.StudentExpiryDate = calculatedStudentExpiryDate;
                    existingStudentLink.TimeCount = vm.Type == (int)ScheduleType.OneTime ? 1 : vm.TimeCount;
                    await _unitOfWork.StudentAppointmentRepository.EditAsync(existingStudentLink);
                }
                else
                {
                    // لو طالب جديد على الموعد ده، ننشئ سجل ربط جديد
                    var studentAppointment = new StudentAppointment
                    {
                        AppointmentId = appointmentId,
                        StudentId = vm.StudentId,
                        TimeCount = vm.Type == (int)ScheduleType.OneTime ? 1 : vm.TimeCount,
                        StudentExpiryDate = calculatedStudentExpiryDate
                    };
                    await _unitOfWork.StudentAppointmentRepository.CreateAsync(studentAppointment);
                }

                await _unitOfWork.CommitAsync();

                return Json(new { success = true, message = "Successfully processed" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchStudents(string term)
        {

            var students = await _unitOfWork.StudentRepository.GetAsync(
                filter: s => string.IsNullOrEmpty(term) || s.NameEn.Contains(term) || s.NameAr.Contains(term) || s.ApplicationUser.Email!.Contains(term)
            );

            var results = students.Select(s => new
            {
                id = s.Id,
                text = $"{s.NameEn} - {s.NameAr}"
            }).ToList();

            return Json(results);
        }
        public async Task<JsonResult> SearchCourses(string term)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            var courses = await _unitOfWork.CourseRepository.GetAsync(
                filter: c => string.IsNullOrEmpty(term) || c.TitleAr.Contains(term) || c.TitleEn.Contains(term)
            );

            var results = courses.Select(c => new
            {
                id = c.Id,
                text = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ar"
                       ? c.TitleAr : c.TitleEn
            });

            return Json(results);
        }

        public async Task<IActionResult> SearchInstructors(string term, int courseId)
        {
            var instructors = await unitOfWork.InstructorRepository.GetAsync(filter: e =>
            (e.NameAr.Contains(term) || e.NameEn.Contains(term) || e.ApplicationUser.Email!.Contains(term)));


            var insCourse = await unitOfWork.InstructorCourseRepository.GetAsync(filter: e => e.CourseId == courseId);
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
                
                var appointment = await _unitOfWork.AppoinmentRepository.GetOneAsync(e=>e.Id == id);
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

    }
}
