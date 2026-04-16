using DataAccess.Repositories.IRepositories;
using ELClass.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace ELClass.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    [Area("Admin")]
    public class HomePageContentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public static readonly (string Code, string NameAr, string NameEn)[] Countries =
        {
            ("SA", "المملكة العربية السعودية", "Saudi Arabia"),
            ("AE", "الإمارات العربية المتحدة", "United Arab Emirates"),
            ("KW", "الكويت", "Kuwait"),
            ("BH", "البحرين", "Bahrain"),
            ("OM", "عُمان", "Oman"),
            ("EG", "مصر", "Egypt"),
            ("QA", "قطر", "Qatar"),
        };

        public HomePageContentController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            await EnsureAllRecordsExistAsync();
            var allContent = (await _unitOfWork.HomePageContentRepository.GetAsync()).ToList();
            return View(allContent);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(HomePageContent model, string tabId , IFormFile PlayVideoImg)
        {
            var isArabic = CultureHelper.IsArabic;

            if(PlayVideoImg != null && PlayVideoImg.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(PlayVideoImg.FileName)}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "thumbnails", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await PlayVideoImg.CopyToAsync(stream);
                }
                model.PlayVideoText = $"{fileName}";
                var allRecords = await _unitOfWork.HomePageContentRepository.GetAsync();
                foreach (var record in allRecords)
                {
                    record.PlayVideoText = fileName;
                    await _unitOfWork.HomePageContentRepository.EditAsync(record);
                }
            }

            await _unitOfWork.HomePageContentRepository.EditAsync(model);
            TempData["Success"] = isArabic ? "تم حفظ المحتوى بنجاح" : "Content saved successfully";
            return RedirectToAction("Index", new { activeTab = tabId });
        }

        private async Task EnsureAllRecordsExistAsync()
        {
            var all = (await _unitOfWork.HomePageContentRepository.GetAsync()).ToList();

            if (!all.Any(x => x.Language == "en"))
                await _unitOfWork.HomePageContentRepository.CreateAsync(new HomePageContent { Language = "en" });

            foreach (var (code, _, _) in Countries)
            {
                if (!all.Any(x => x.Language == "ar" && x.CountryCode == code))
                    await _unitOfWork.HomePageContentRepository.CreateAsync(new HomePageContent { Language = "ar", CountryCode = code });
            }
        }
    }
}
