using DataAccess.Repositories.IRepositories;
using ELClass.services;
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
            var all = (await _unitOfWork.ContactUsRepository.GetAsync())
                .OrderByDescending(c => c.Id)
                .ToList();

            var unreadIds = all.Where(c => !c.IsReaded).Select(c => c.Id).ToHashSet();
            int unreadCount = unreadIds.Count;

            if (unreadCount > 0)
            {
                foreach (var c in all.Where(c => !c.IsReaded))
                    c.IsReaded = true;
                await _unitOfWork.ContactUsRepository.EditAllAsync(all.AsQueryable());
                await _unitOfWork.CommitAsync();
            }

            ViewBag.UnreadCount = unreadCount;
            ViewBag.UnreadIds   = unreadIds;

            return View(all);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var model = await _unitOfWork.ContactUsRepository.GetOneAsync(c => c.Id == id);
            if (model == null) return NotFound();

            await _unitOfWork.ContactUsRepository.DeleteAsync(model);
            await _unitOfWork.CommitAsync();

            bool isArabic = CultureHelper.IsArabic;
            TempData["success"] = isArabic ? "تم حذف الرسالة بنجاح." : "Message deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
