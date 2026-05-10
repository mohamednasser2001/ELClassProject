using DataAccess.Repositories.IRepositories;
using ELClass.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace ELClass.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    [Area("Admin")]
    public class FAQsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public static readonly (string Code, string NameAr, string NameEn)[] Countries =
            HomePageContentController.Countries;

        public FAQsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(string lang = "en", string? country = null)
        {
            var items = (await _unitOfWork.FAQRepository.GetAsync(
                filter: x => x.Language == lang && x.CountryCode == country,
                orderBy: q => q.OrderBy(x => x.SortOrder),
                tracked: false)).ToList();

            ViewBag.Lang = lang;
            ViewBag.Country = country;
            ViewBag.Countries = Countries;
            return View(items);
        }

        [HttpGet]
        public IActionResult Create(string lang = "en", string? country = null)
        {
            ViewBag.Lang = lang;
            ViewBag.Country = country;
            ViewBag.Countries = Countries;
            return View(new FAQ { Language = lang, CountryCode = country });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FAQ model)
        {
            ModelState.Remove("Language");
            if (!ModelState.IsValid)
            {
                ViewBag.Lang = model.Language;
                ViewBag.Country = model.CountryCode;
                ViewBag.Countries = Countries;
                return View(model);
            }
            await _unitOfWork.FAQRepository.CreateAsync(model);
            await _unitOfWork.CommitAsync();
            TempData["Success"] = CultureHelper.IsArabic ? "تم إضافة السؤال بنجاح" : "FAQ added successfully";
            return RedirectToAction("Index", new { lang = model.Language, country = model.CountryCode });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _unitOfWork.FAQRepository.GetOneAsync(x => x.Id == id);
            if (item == null) return NotFound();
            ViewBag.Lang = item.Language;
            ViewBag.Country = item.CountryCode;
            ViewBag.Countries = Countries;
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FAQ model)
        {
            ModelState.Remove("Language");
            if (!ModelState.IsValid)
            {
                ViewBag.Lang = model.Language;
                ViewBag.Country = model.CountryCode;
                ViewBag.Countries = Countries;
                return View(model);
            }
            await _unitOfWork.FAQRepository.EditAsync(model);
            await _unitOfWork.CommitAsync();
            TempData["Success"] = CultureHelper.IsArabic ? "تم تحديث السؤال بنجاح" : "FAQ updated successfully";
            return RedirectToAction("Index", new { lang = model.Language, country = model.CountryCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _unitOfWork.FAQRepository.GetOneAsync(x => x.Id == id);
            if (item != null)
            {
                await _unitOfWork.FAQRepository.DeleteAsync(item);
                await _unitOfWork.CommitAsync();
                TempData["Success"] = CultureHelper.IsArabic ? "تم حذف السؤال" : "FAQ deleted";
                return RedirectToAction("Index", new { lang = item.Language, country = item.CountryCode });
            }
            return RedirectToAction("Index");
        }
    }
}
