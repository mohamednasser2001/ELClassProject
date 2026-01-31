using DataAccess.Repositories.IRepositories;
using ELClass.services;
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
            var appointments = await _unitOfWork.AppoinmentRepository.GetAsync(e => e.InstructorId == userId, include: e => e.Include(e => e.Course!).Include(e => e.StudentAppointments));
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

                var existingAppointment = await _unitOfWork.AppoinmentRepository.GetOneAsync(a =>
                    a.InstructorId == vm.InstructorId &&
                    a.CourseId == vm.CourseId &&
                    a.Type == (ScheduleType)vm.Type &&
                    a.StartTime == vm.StartTime &&
                    (vm.Type == (int)ScheduleType.Recurring ? a.Day == vm.Day : a.SpecificDate == vm.SpecificDate)
                );

                if (existingAppointment != null)
                {
                    return Json(new { success = false, message = "هذا الموعد موجود بالفعل لهذا المدرس" });
                }

                // 2. إنشاء الموعد الجديد
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

                return Json(new { success = true, message = "تم إنشاء الموعد بنجاح" });
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
            if (ModelState.IsValid)
            {
                var existing = await _unitOfWork.AppoinmentRepository.GetOneAsync(e => e.Id == model.Id);
                if (existing == null) return NotFound();


                existing.Type = model.Type;
                existing.Day = model.Day;
                existing.StartTime = model.StartTime;
                existing.SpecificDate = model.SpecificDate;
                existing.DurationInHours = model.DurationInHours;
                existing.MeetingLink = model.MeetingLink;

                await _unitOfWork.AppoinmentRepository.EditAsync(existing);
                await _unitOfWork.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(model);
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
