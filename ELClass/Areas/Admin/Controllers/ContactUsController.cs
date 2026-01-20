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
            var contacts = (await _unitOfWork.ContactUsRepository.GetAsync()).AsQueryable().OrderByDescending(c => c.Id);
            if (contacts.Any())
            {
                foreach (var contact in contacts)
                {
                    contact.IsReaded = true;
                }
                await _unitOfWork.ContactUsRepository.EditAllAsync(contacts);
                await _unitOfWork.CommitAsync();
            }

            return View(contacts);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentLang = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            bool isArabic = currentLang == "ar";

            var model = await _unitOfWork.ContactUsRepository.GetOneAsync(c => c.Id == id);

            if(model == null)
            {
                return NotFound();
            }

            await _unitOfWork.ContactUsRepository.DeleteAsync(model);
            TempData["success"] = isArabic ? "تم حذف الرسالة  بنجاح." : "Message deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
