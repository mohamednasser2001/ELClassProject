using System.Threading.Tasks;
using DataAccess;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Models;
using Models.ViewModels;
//using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace ELClass.Areas.Identity.Controllers
{

    [Area("Identity")]
    public class AccountController : Controller
    {
       private ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private UserManager<ApplicationUser> _userManager;

        public AccountController(UserManager<ApplicationUser> userManager,
            ApplicationDbContext _context,SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            this._context = _context;
            this._signInManager = signInManager;
        }

        [HttpPost]
        public IActionResult SetLanguage(string language)
        {

            HttpContext.Session.SetString("Language", language);

            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(registerVM);
                #region check model state with language
                var lang = HttpContext.Session.GetString("Language") ?? "en";

                // التحقق من الاسم حسب اللغة
                if ((lang == "en" && string.IsNullOrWhiteSpace(registerVM.NameEN)) ||
                    (lang == "ar" && string.IsNullOrWhiteSpace(registerVM.NameAR)))
                {
                    ModelState.AddModelError(
                        lang == "en" ? "NameEN" : "NameAR",
                        lang == "en" ? "Name is required" : "الاسم مطلوب"
                    );
                }

                // التحقق من باقي الحقول المهمة (مثال: Email و Password)
                if (string.IsNullOrWhiteSpace(registerVM.Email))
                {
                    ModelState.AddModelError("Email", "Email is required");
                }
                if (string.IsNullOrWhiteSpace(registerVM.Password))
                {
                    ModelState.AddModelError("Password", "Password is required");
                }


                if (!ModelState.IsValid)
                {
                    return View(registerVM);
                }
                #endregion

                ApplicationUser applicationUser = new ApplicationUser()
                {
                    NameEN = registerVM.NameEN,
                    NameAR = registerVM.NameAR,
                    Email = registerVM.Email,
                    UserName = registerVM.Email,
                    


                };
                var result = await _userManager.CreateAsync(applicationUser, registerVM.Password);
                if (!result.Succeeded)
                {

                    TempData["error-notification"] = result.Errors.Select(e => e.Code);

                    return View(registerVM);
                }

                TempData["success-notification"] = "Add Account Successfully";

                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = Request.Form["draw"].FirstOrDefault(),
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = ex.Message
                });
            }

         

         
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    // إعدادات قفل الحساب (Lockout) اعمله true لما تخلص
                    var result = await _signInManager.PasswordSignInAsync
                        (loginVM.EmailOrUserName, loginVM.Password, loginVM.RememberMe, false);
                    if (result.Succeeded)
                    {
                      
                        var currentLang = HttpContext.Session.GetString("Language") ?? "en";
                        TempData["success-notifications"] = currentLang == "ar" ? "تم تسجيل الدخول بنجاح" : "Logged in successfully";

                        return RedirectToAction("index", "Course", new {area="admin"});
                    }
               
                    var lang = HttpContext.Session.GetString("Language") ?? "en";
                    ModelState.AddModelError(string.Empty, lang == "ar"
                        ? "محاولة دخول غير صالحة. تأكد من البيانات."
                        : "Invalid login attempt.");
                }
                return View(loginVM);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = Request.Form["draw"].FirstOrDefault(),
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = ex.Message
                });
            }


            
        }


    }
}
