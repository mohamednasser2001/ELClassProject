using System;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models; // حسب مكان ApplicationUser

namespace ELClass.Areas.StudentArea.Controllers
{
    [Area("StudentArea")]
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentsController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Join(int studentAppointmentId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

           
            var saList = await _unitOfWork.StudentAppointmentRepository.GetAsync(
                x => x.Id == studentAppointmentId && x.StudentId == userId,
                q => q.Include(a => a.Appointment)
            );

            var sa = saList.FirstOrDefault();
            if (sa == null)
                return NotFound();

           
            var appointment = await _unitOfWork.AppoinmentRepository.GetOneAsync(a => a.Id == sa.AppointmentId);
            if (appointment == null)
                return NotFound();

           
            if (string.IsNullOrWhiteSpace(appointment.MeetingLink))
                return BadRequest("Meeting link is missing.");

            if (!appointment.IsActive)
                return BadRequest("This session is not active yet.");

          
            if (!sa.IsAttended)
            {
                sa.IsAttended = true;
                sa.AttendedAt = DateTime.UtcNow;

                _unitOfWork.StudentAppointmentRepository.EditAsync(sa);
                await _unitOfWork.CommitAsync(); 
            }

      
            return Redirect(appointment.MeetingLink);
        }

    }
}

