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
            var appointments = await _unitOfWork.AppoinmentRepository.GetAsync(e => e.InstructorId == userId, include: e => e.Include(e => e.Course!));
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

                var appointment = new Appointment
                {
                    InstructorId = vm.InstructorId,
                    MeetingLink = vm.MeetingLink,
                    Type = (ScheduleType)vm.Type,
                    DurationInHours = vm.DurationInHours,
                    StartTime = vm.StartTime,
                    CourseId = vm.CourseId,

                    Day = vm.Type == 0 ? vm.Day : 0,
                    SpecificDate = vm.Type == 1 ? vm.SpecificDate : null
                };

                await _unitOfWork.AppoinmentRepository.CreateAsync(appointment);
                await _unitOfWork.CommitAsync();



                var StudentAppointment = new StudentAppointment()
                {
                    AppointmentId = appointment.Id,
                    StudentId = vm.StudentId,
                    TimeCount = vm.Type == 1 ? 1 : vm.TimeCount
                };


                await _unitOfWork.StudentAppointmentRepository.CreateAsync(StudentAppointment);
                await _unitOfWork.CommitAsync();


                return Json(new { success = true, message = "Created Successfully" });
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
                // 1. البحث عن الموعد
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
