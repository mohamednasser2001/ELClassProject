using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ELClass.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ContactUsController(IUnitOfWork unitOfWork) : Controller
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<IActionResult> Index()
        {
            var contacts = (await _unitOfWork.ContactUsRepository.GetAsync()).OrderByDescending(c => c.Id);
            



            return View(contacts);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var model = await _unitOfWork.ContactUsRepository.GetOneAsync(c => c.Id == id);

            if(model == null)
            {
                return NotFound();
            }

            await _unitOfWork.ContactUsRepository.DeleteAsync(model);
            return RedirectToAction(nameof(Index));
        }
    }
}
