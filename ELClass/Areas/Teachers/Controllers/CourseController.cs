using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;

namespace ELClass.Areas.Teacher.Controllers
{
    [Area("Teachers")]
   // [Authorize(Roles = "Teacher")]
    public class CourseController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourseController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            this._unitOfWork = unitOfWork;
            this._userManager = userManager;
        }
        public IActionResult ChangeLanguage(string lang)
        {
            // حفظ اللغة في الـ Session أو الـ Cookie
            HttpContext.Session.SetString("Language", lang);

            // العودة للصفحة السابقة
            return Redirect(Request.Headers["Referer"].ToString());
        }
        public async Task<IActionResult> Index()
        {
            // 1. الحصول على Id المدرس الحالي
            var userId = _userManager.GetUserId(User);

            // 2. جلب الكورسات المرتبطة بهذا المدرس فقط
            // سنستخدم GetAsync مع Include لجدول الربط ثم جدول الكورس نفسه
            var myCoursesLinks = await _unitOfWork.InstructorCourseRepository.GetAsync(
               e=> e.InstructorId == userId,
                include: source => source.Include(e => e.Course)
              );

            // 3. استخراج قائمة الكورسات من جدول الربط لعرضها في الـ View
            var courses = myCoursesLinks.Select(ic => ic.Course).ToList();

            return View(courses);
           
        }
    }
}
