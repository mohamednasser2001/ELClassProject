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
using Microsoft.AspNetCore.Authentication;

using System.Security.Claims;
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
        public IActionResult Register(string? email = null)
        {
            
            ViewBag.GoogleEmail = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM registerVM, string? returnUrl = null)
        {
            try
            {
                returnUrl ??= Url.Content("~/");

                // التحقق من النموذج
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

                // لو المستخدم جاي من Google، Password ممكن يكون فاضي
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null && string.IsNullOrWhiteSpace(registerVM.Password))
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
                    UserName = registerVM.Email.Split('@')[0] + new Random().Next(10, 99).ToString(),
                };

                IdentityResult result;

                if (info != null)
                {
                    // المستخدم جاي من Google → نسجل بدون باسورد
                    result = await _userManager.CreateAsync(applicationUser);
                }
                else
                {
                    // التسجيل العادي
                    result = await _userManager.CreateAsync(applicationUser, registerVM.Password);
                }

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(applicationUser, "Student");

                    var std = new Student()
                    {
                        Id = applicationUser.Id,
                        NameAr = applicationUser.NameAR ?? "",
                        NameEn = applicationUser.NameEN ?? ""
                    };
                    await unitOfWork.StudentRepository.CreateAsync(std);
                    await unitOfWork.CommitAsync();

                    // لو المستخدم جاي من Google → نربط الحساب
                    if (info != null)
                    {
                        await _userManager.AddLoginAsync(applicationUser, info);
                        await _signInManager.SignInAsync(applicationUser, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }

                    // التسجيل العادي → تأكيد الإيميل
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

           
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction("Login");

            
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Account",
                new { userId = user.Id, token = token }, Request.Scheme);

           
            var lang = HttpContext.Session.GetString("Language") ?? "en";
            await _emailSender.SendEmailAsync(user.Email!,
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
                        }else if (roles.Contains("Instructor"))
                        {
                            return RedirectToAction("index", "Course", new { area = "Instructor" });
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

          
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

              
                var callbackUrl = Url.Action("ResetPassword", "Account",
                    new { userId = user.Id, token = token }, Request.Scheme);

                await _emailSender.SendEmailAsync(email, "Reset Password",
                    $"لإعادة تعيين كلمة المرور، اضغط هنا: <a href='{callbackUrl}'>Reset Password</a>");
            }

         
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

   
            var result = await _userManager.ResetPasswordAsync(user, resetPasswordVM.Token, resetPasswordVM.NewPassword);

            if (result.Succeeded)
            {
                TempData["success-notifications"] = "Password has been reset successfully!";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(resetPasswordVM);
        }
        //login by google
        [HttpGet]
        public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = null)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
      
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return RedirectToAction("Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction("Login");
            }

            // محاولة تسجيل دخول لو الحساب مربوط قبل كده فقط
            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                return LocalRedirect(returnUrl ?? "/");
            }

            // لو الحساب جديد، نرسل المستخدم لصفحة GoogleResponse للتحقق
            return RedirectToAction("GoogleResponse", new { returnUrl });
        }


        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string returnUrl = null)
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            if (signInResult.Succeeded)
                return LocalRedirect(returnUrl ?? "/");

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);

            if (!string.IsNullOrEmpty(email))
            {
                var user = await _userManager.FindByEmailAsync(email);

                if (user != null)
                {
                    var result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl ?? "/");
                    }
                }
                else
                {
                    // المستخدم جديد → نوديه على صفحة Register مع تمرير الإيميل
                    return RedirectToAction("Register", "Account", new { Email = email });
                }
            }

            return RedirectToAction(nameof(Login));
        }



    }
}
