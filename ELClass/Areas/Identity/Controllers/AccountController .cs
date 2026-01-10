using System.Threading.Tasks;
using DataAccess;
using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
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
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork unitOfWork;
        private UserManager<ApplicationUser> _userManager;

        public AccountController(UserManager<ApplicationUser> userManager,
            ApplicationDbContext _context,
            SignInManager<ApplicationUser> signInManager, IEmailSender emailSender, RoleManager<IdentityRole> roleManager
            , IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            this._context = _context;
            this._signInManager = signInManager;
            this._emailSender = emailSender;
            this._roleManager = roleManager;
            this.unitOfWork = unitOfWork;
        }
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
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

                if ((lang == "en" && string.IsNullOrWhiteSpace(registerVM.NameEN)) ||
                    (lang == "ar" && string.IsNullOrWhiteSpace(registerVM.NameAR)))
                {
                    ModelState.AddModelError(
                        lang == "en" ? "NameEN" : "NameAR",
                        lang == "en" ? "Name is required" : "الاسم مطلوب"
                    );
                }

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

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(applicationUser, "Student");

                    var std = new Student() { Id = applicationUser.Id ,NameAr = applicationUser.NameAR ?? "", NameEn = applicationUser.NameEN ?? "" };
                    await unitOfWork.StudentRepository.CreateAsync(std);
                    await unitOfWork.CommitAsync();
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(applicationUser);

                    
                    var confirmationLink = Url.Action("ConfirmEmail", "Account",
                        new { userId = applicationUser.Id, token = token }, Request.Scheme);

                    
                    await _emailSender.SendEmailAsync(applicationUser.Email,
                        lang == "ar" ? "تأكيد الحساب" : "Confirm Your Email",
                        lang == "ar" ? $"برجاء تأكيد حسابك من خلال <a href='{confirmationLink}'>الضغط هنا</a>"
                                     : $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

                    TempData["success-notification"] = lang == "ar"
                        ? "تم إنشاء الحساب! برجاء مراجعة بريدك الإلكتروني لتفعيله."
                        : "Account Created! Please check your email to confirm.";

                    return RedirectToAction(nameof(Login));
                }
                else
                {
                  
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(registerVM);
                }
            }
            catch (Exception ex)
            {
               
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(registerVM);
            }




        }
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId,string token)
        {
            if (userId == null || token == null) return RedirectToAction("Login", "Account");

            var user = await _userManager.FindByIdAsync(userId);
         
            if (user == null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                TempData["success-notifications"] = "Email confirmed successfully! You can now login.";
                return RedirectToAction("Login");
            }

            return View("Error");
        }

        [HttpGet]
        public async Task<IActionResult> ResendConfirmationEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            // 1. البحث عن المستخدم
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction("Login");

            // 2. توليد رابط تفعيل جديد
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Account",
                new { userId = user.Id, token = token }, Request.Scheme);

            // 3. إرسال الإيميل باستخدام الـ EmailSender اللي عملناه
            var lang = HttpContext.Session.GetString("Language") ?? "en";
            await _emailSender.SendEmailAsync(user.Email,
                lang == "ar" ? "تفعيل الحساب" : "Confirm Your Email",
                lang == "ar" ? $"رابط التفعيل الجديد: <a href='{confirmationLink}'>إضغط هنا</a>"
                             : $"New activation link: <a href='{confirmationLink}'>Click here</a>.");

            TempData["success-notifications"] = lang == "ar" ? "تم إرسال رابط التفعيل بنجاح" : "Activation link sent successfully";
            return RedirectToAction("Login");
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
                    var user = await _userManager.FindByEmailAsync(loginVM.EmailOrUserName)
                                        ?? await _userManager.FindByNameAsync(loginVM.EmailOrUserName);

                    if (user == null)
                    {
                        ModelState.AddModelError("", "Invalid login attempt");
                        return View(loginVM);
                    }

                    var result = await _signInManager.PasswordSignInAsync
                        (user, loginVM.Password, loginVM.RememberMe, true);

                    if (result.Succeeded)
                    {

                        var currentLang = HttpContext.Session.GetString("Language") ?? "en";
                        TempData["success-notifications"] = currentLang == "ar" ? "تم تسجيل الدخول بنجاح" : "Logged in successfully";

                        //var user = await _userManager.FindByEmailAsync(loginVM.EmailOrUserName) ?? await _userManager.FindByNameAsync(loginVM.EmailOrUserName);
                        var roles = await _userManager.GetRolesAsync(user!);
                        if (roles.Contains("Admin") || roles.Contains("SuperAdmin"))
                        {
                            return RedirectToAction("index", "home", new { area = "admin" });
                        }else if (roles.Contains("Teacher"))
                        {
                            return RedirectToAction("index", "Course", new { area = "Teachers" });
                        }
                        return RedirectToAction("index", "Home", new { area = "StudentArea" });
                    }

                    
                    var lang = HttpContext.Session.GetString("Language") ?? "en";

                    
                    if (result.IsNotAllowed)
                    {
                        ModelState.AddModelError(string.Empty, lang == "ar"
                            ? "يجب تأكيد البريد الإلكتروني أولاً لتتمكن من الدخول."
                            : "You must confirm your email to log in.");
                    }
                    
                    else if (result.IsLockedOut)
                    {
                        ModelState.AddModelError(string.Empty, lang == "ar"
                            ? "هذا الحساب مغلق مؤقتاً بسبب محاولات دخول خاطئة كثيرة."
                            : "This account is locked out due to too many failed attempts.");
                    }
                   
                    else
                    {
                        ModelState.AddModelError(string.Empty, lang == "ar"
                            ? "خطأ في البريد الإلكتروني أو كلمة المرور."
                            : "Invalid login attempt.");
                    }
                }

                return View(loginVM);
            }
            catch (Exception ex)
            {
               
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(loginVM);
            }



        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email)) return View();

            var user = await _userManager.FindByEmailAsync(email);

            // حتى لو المستخدم غير موجود، لا نخبر المخترقين بذلك (أمان)
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                // توليد توكن إعادة تعيين كلمة المرور
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // إنشاء الرابط الذي سيوجه المستخدم لصفحة كتابة الباسوورد الجديد
                var callbackUrl = Url.Action("ResetPassword", "Account",
                    new { userId = user.Id, token = token }, Request.Scheme);

                await _emailSender.SendEmailAsync(email, "Reset Password",
                    $"لإعادة تعيين كلمة المرور، اضغط هنا: <a href='{callbackUrl}'>Reset Password</a>");
            }

            // نظهر رسالة نجاح دائماً لزيادة الأمان
            TempData["success-notifications"] = "Check your email to reset your password.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Login");
            }

            // نقوم بتجهيز الـ VM بالبيانات القادمة من الرابط
            var model = new ResetPasswordVM
            {
                UserId = userId,
                Token = token
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM resetPasswordVM)
        {
            if (!ModelState.IsValid) return View(resetPasswordVM);

            var user = await _userManager.FindByIdAsync(resetPasswordVM.UserId);
            if (user == null) return RedirectToAction("Login");

            // السطر السحري الذي يغير الباسوورد في قاعدة البيانات باستخدام التوكن
            var result = await _userManager.ResetPasswordAsync(user, resetPasswordVM.Token, resetPasswordVM.NewPassword);

            if (result.Succeeded)
            {
                TempData["success-notifications"] = "Password has been reset successfully!";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(resetPasswordVM);
        }
    }
}
