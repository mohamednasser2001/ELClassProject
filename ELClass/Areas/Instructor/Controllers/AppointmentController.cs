using DataAccess.Repositories.IRepositories;
using ELClass.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class AppointmentController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public async Task<IActionResult> Index()
        {

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();



            var appointments = (await _unitOfWork.AppoinmentRepository.GetAsync(
                filter: e => e.InstructorId == userId,
                include: e => e.Include(e => e.Course!)
                              .Include(e => e.StudentAppointments)
            )).ToList();

            var now = DateTime.Now;


            var orderedAppointments = appointments
        .OrderBy(a => a.StartDateTime)
        .ToList();
            return View(orderedAppointments);
        }




        [HttpGet]
        public async Task<IActionResult> SearchStudents(string term, int courseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            var studentCourses = await _unitOfWork.StudentCourseRepository.GetAsync(
                filter: sc => sc.CourseId == courseId &&
                              (string.IsNullOrEmpty(term) ||
                               sc.Student.NameEn.Contains(term) ||
                               sc.Student.NameAr.Contains(term) ||
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
                          ic.Course.TitleEn.Contains(term)), include: ic => ic.Include(ic => ic.Course)
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



        [HttpPost]
        public async Task<IActionResult> Edit(int id, DateTime startDateTime, int durationInHours, string meetingLink)
        {
            try
            {
                var existing = await _unitOfWork.AppoinmentRepository.GetOneAsync(e => e.Id == id);
                if (existing == null)
                    return Json(new { success = false, message = CultureHelper.IsArabic ? "الموعد غير موجود" : "Appointment not found" });

                // التحقق من التعارض
                var slotEnd = startDateTime.AddHours(durationInHours);
                var conflicts = await _unitOfWork.AppoinmentRepository
                    .GetAsync(a => a.Id != id && a.InstructorId == existing.InstructorId);

                var conflict = conflicts.FirstOrDefault(a =>
                    startDateTime < a.EndDateTime && slotEnd > a.StartDateTime);

                if (conflict != null)
                    return Json(new
                    {
                        success = false,
                        message = (CultureHelper.IsArabic ? "يوجد تعارض في: " : "Conflict at: ")
                                  + conflict.StartDateTime.ToString("dd/MM/yyyy hh:mm tt")
                    });

                existing.StartDateTime = startDateTime;
                existing.DurationInHours = durationInHours;
                existing.MeetingLink = meetingLink;

                await _unitOfWork.AppoinmentRepository.EditAsync(existing);
                await _unitOfWork.CommitAsync();

                return Json(new { success = true, message = CultureHelper.IsArabic ? "تم التعديل بنجاح" : "Updated successfully" });
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

            if (appointment.StartDateTime.Date < DateTime.Now.Date)
            {
                return Json(new { success = false, message = "لا يمكن إضافة طلاب لموعد قديم" });
            }

            DateTime? expiryDate = null;



            expiryDate = appointment.EndDateTime;

            var link = new StudentAppointment
            {
                StudentId = StudentId,
                AppointmentId = AppointmentId,

            };


            await _unitOfWork.StudentAppointmentRepository.CreateAsync(link);
            await _unitOfWork.CommitAsync();
            TempData["Success"] = CultureHelper.IsArabic ? "تم اضافة الطالب بنجاح " : " the student has been added successfully";
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveStudentFromAppointment(int studentAppointmentId)
        {
            var item = await _unitOfWork.StudentAppointmentRepository.GetOneAsync(e => e.Id == studentAppointmentId);
            if (item != null)
            {
                await _unitOfWork.StudentAppointmentRepository.DeleteAsync(item);
                await _unitOfWork.CommitAsync();

                return Json(new { success = true });
            }

            return Json(new { success = false });
        }


        [HttpGet]
        public async Task<IActionResult> GetAppointmentStudents(int appointmentId)
        {
            var appointment = await _unitOfWork.AppoinmentRepository.GetOneAsync(
                a => a.Id == appointmentId,
                include: e => e.Include(e => e.StudentAppointments)
                               .ThenInclude(sa => sa.Student!)
            );

            if (appointment == null) return Json(new List<object>());

            var result = appointment.StudentAppointments.Select(sa => new
            {
                studentId = sa.StudentId,
                name = CultureHelper.IsArabic
                    ? sa.Student!.NameAr
                    : sa.Student!.NameEn
            });

            return Json(result);
        }

       


    }
}
