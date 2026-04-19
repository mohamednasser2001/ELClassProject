using DataAccess.Repositories.IRepositories;
using ELClass.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace ELClass.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    [Area("Admin")]
    public class TestimonialsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public static readonly (string Code, string NameAr, string NameEn)[] Countries =
            HomePageContentController.Countries;

        public TestimonialsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(string lang = "en", string? country = null)
        {
            var items = (await _unitOfWork.TestimonialRepository.GetAsync(
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
            return View(new Testimonial { Language = lang, CountryCode = country });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Testimonial model, IFormFile? ImageFile)
        {
            if (ImageFile != null && ImageFile.Length > 0)
                model.ImageFileName = await SaveImageAsync(ImageFile, "testimonials");

            await _unitOfWork.TestimonialRepository.CreateAsync(model);
            await _unitOfWork.CommitAsync();

            var isAr = CultureHelper.IsArabic;
            TempData["Success"] = isAr ? "تم إضافة الشهادة بنجاح" : "Testimonial created successfully";
            return RedirectToAction("Index", new { lang = model.Language, country = model.CountryCode });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _unitOfWork.TestimonialRepository.GetOneAsync(x => x.Id == id);
            if (item == null) return NotFound();
            ViewBag.Lang = item.Language;
            ViewBag.Country = item.CountryCode;
            ViewBag.Countries = Countries;
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Testimonial model, IFormFile? ImageFile)
        {
            if (ImageFile != null && ImageFile.Length > 0)
                model.ImageFileName = await SaveImageAsync(ImageFile, "testimonials");

            await _unitOfWork.TestimonialRepository.EditAsync(model);
            await _unitOfWork.CommitAsync();

            var isAr = CultureHelper.IsArabic;
            TempData["Success"] = isAr ? "تم تحديث الشهادة بنجاح" : "Testimonial updated successfully";
            return RedirectToAction("Index", new { lang = model.Language, country = model.CountryCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _unitOfWork.TestimonialRepository.GetOneAsync(x => x.Id == id);
            if (item != null)
            {
                await _unitOfWork.TestimonialRepository.DeleteAsync(item);
                await _unitOfWork.CommitAsync();
                var isAr = CultureHelper.IsArabic;
                TempData["Success"] = isAr ? "تم حذف الشهادة" : "Testimonial deleted";
                return RedirectToAction("Index", new { lang = item.Language, country = item.CountryCode });
            }
            return RedirectToAction("Index");
        }

        private async Task<string> SaveImageAsync(IFormFile file, string folder)
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder);
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return fileName;
        }
    }
}
