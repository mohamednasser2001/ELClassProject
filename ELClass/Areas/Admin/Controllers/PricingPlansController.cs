using DataAccess.Repositories.IRepositories;
using ELClass.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace ELClass.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    [Area("Admin")]
    public class PricingPlansController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public static readonly (string Code, string NameAr, string NameEn)[] Countries =
            HomePageContentController.Countries;

        // Default pricing plans seeded on first visit
        private static readonly (string NameEn, string NameAr, string DescEn, string DescAr, string Monthly, string Yearly, bool IsPopular, string FeaturesEn, string FeaturesAr)[] Defaults =
        {
            (
                "Basic", "أساسي",
                "For an individual and beginner", "للمبتدئين والطلاب الجدد",
                "199", "1900", false,
                "One year premium access|1\nUnlimited courses|1\n1000+ lessons & growing|1\nRandom supporter|0\nFree eBook downloads|0\nPremium tutorials|0\nUnlimited registered user|0",
                "وصول متميز لمدة سنة واحدة|1\nدورات غير محدودة|1\n+1000 درس ومتزايد|1\nدعم عشوائي|0\nتحميلات كتب مجانية|0\nدروس متميزة|0\nمستخدمين غير محدودين|0"
            ),
            (
                "Standard", "قياسي",
                "For an individual and beginner", "للطلاب المتوسطين",
                "299", "2900", true,
                "One year premium access|1\nUnlimited courses|1\n1000+ lessons & growing|1\nRandom supporter|1\nFree eBook downloads|1\nPremium tutorials|0\nUnlimited registered user|0",
                "وصول متميز لمدة سنة واحدة|1\nدورات غير محدودة|1\n+1000 درس ومتزايد|1\nدعم عشوائي|1\nتحميلات كتب مجانية|1\nدروس متميزة|0\nمستخدمين غير محدودين|0"
            ),
            (
                "Premium", "متميز",
                "For an individual and beginner", "للطلاب المحترفين",
                "399", "3900", false,
                "One year premium access|1\nUnlimited courses|1\n1000+ lessons & growing|1\nRandom supporter|1\nFree eBook downloads|1\nPremium tutorials|1\nUnlimited registered user|1",
                "وصول متميز لمدة سنة واحدة|1\nدورات غير محدودة|1\n+1000 درس ومتزايد|1\nدعم عشوائي|1\nتحميلات كتب مجانية|1\nدروس متميزة|1\nمستخدمين غير محدودين|1"
            )
        };

        public PricingPlansController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(string lang = "en", string? country = null)
        {
            await EnsureDefaultsAsync(lang, country);

            var items = (await _unitOfWork.PricingPlanRepository.GetAsync(
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
            return View(new PricingPlan { Language = lang, CountryCode = country, Currency = lang == "en" ? "$" : "ر.س" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PricingPlan model)
        {
            await _unitOfWork.PricingPlanRepository.CreateAsync(model);
            await _unitOfWork.CommitAsync();
            var isAr = CultureHelper.IsArabic;
            TempData["Success"] = isAr ? "تم إضافة الخطة بنجاح" : "Pricing plan created successfully";
            return RedirectToAction("Index", new { lang = model.Language, country = model.CountryCode });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _unitOfWork.PricingPlanRepository.GetOneAsync(x => x.Id == id);
            if (item == null) return NotFound();
            ViewBag.Lang = item.Language;
            ViewBag.Country = item.CountryCode;
            ViewBag.Countries = Countries;
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PricingPlan model)
        {
            await _unitOfWork.PricingPlanRepository.EditAsync(model);
            await _unitOfWork.CommitAsync();
            var isAr = CultureHelper.IsArabic;
            TempData["Success"] = isAr ? "تم تحديث الخطة بنجاح" : "Pricing plan updated successfully";
            return RedirectToAction("Index", new { lang = model.Language, country = model.CountryCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _unitOfWork.PricingPlanRepository.GetOneAsync(x => x.Id == id);
            if (item != null)
            {
                await _unitOfWork.PricingPlanRepository.DeleteAsync(item);
                await _unitOfWork.CommitAsync();
                var isAr = CultureHelper.IsArabic;
                TempData["Success"] = isAr ? "تم حذف الخطة" : "Plan deleted";
                return RedirectToAction("Index", new { lang = item.Language, country = item.CountryCode });
            }
            return RedirectToAction("Index");
        }

        private async Task EnsureDefaultsAsync(string lang, string? country)
        {
            var existing = await _unitOfWork.PricingPlanRepository.CountAsync(
                x => x.Language == lang && x.CountryCode == country);
            if (existing > 0) return;

            bool isAr = lang == "ar";
            int order = 0;
            foreach (var d in Defaults)
            {
                await _unitOfWork.PricingPlanRepository.CreateAsync(new PricingPlan
                {
                    Language = lang,
                    CountryCode = country,
                    Name = isAr ? d.NameAr : d.NameEn,
                    Description = isAr ? d.DescAr : d.DescEn,
                    MonthlyPrice = d.Monthly,
                    YearlyPrice = d.Yearly,
                    Currency = isAr ? "ر.س" : "$",
                    IsPopular = d.IsPopular,
                    Features = isAr ? d.FeaturesAr : d.FeaturesEn,
                    SortOrder = order++,
                    IsActive = true
                });
            }
            await _unitOfWork.CommitAsync();
        }
    }
}
