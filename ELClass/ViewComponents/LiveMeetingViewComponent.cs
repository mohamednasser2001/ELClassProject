using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ELClass.ViewComponents
{
    public class LiveMeetingViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _unitOfWork;
        public LiveMeetingViewComponent(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = ((ClaimsPrincipal)User).FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId)) return Content("");

            
            var appointments = await _unitOfWork.AppoinmentRepository.GetAsync(
                e => e.InstructorId == userId
            );

            
            var activeMeeting = appointments.FirstOrDefault(e => e.IsActive);

            return View(activeMeeting);
        }
    }
}
