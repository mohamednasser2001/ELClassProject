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
            ViewBag.IsExternalRegister = !string.IsNullOrWhiteSpace(email);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM registerVM, string? returnUrl = null)
        {
            // 1) returnUrl safety (prevent open redirect)
            returnUrl ??= Url.Content("~/");
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.Content("~/");

            // 2) language (culture is reliable)
            var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName; // "en" / "ar"

            try
            {
                // 3) External login info early
                var info = await _signInManager.GetExternalLoginInfoAsync();

                // لو جاي من Google: شيل validation الباسورد من ModelState
                if (info != null)
                {
                    ModelState.Remove(nameof(RegisterVM.Password));
                    ModelState.Remove(nameof(RegisterVM.ConfirmPassword));
                }

                #region check model state with language

                // Required name based on language
                if ((lang == "en" && string.IsNullOrWhiteSpace(registerVM.NameEN)) ||
                    (lang == "ar" && string.IsNullOrWhiteSpace(registerVM.NameAR)))
                {
                    ModelState.AddModelError(
                        lang == "en" ? nameof(RegisterVM.NameEN) : nameof(RegisterVM.NameAR),
                        lang == "en" ? "Name is required" : "الاسم مطلوب"
                    );
                }

                // Fill missing name to avoid NULL in DB
                if (string.IsNullOrWhiteSpace(registerVM.NameEN) && !string.IsNullOrWhiteSpace(registerVM.NameAR))
                    registerVM.NameEN = registerVM.NameAR;

                if (string.IsNullOrWhiteSpace(registerVM.NameAR) && !string.IsNullOrWhiteSpace(registerVM.NameEN))
                    registerVM.NameAR = registerVM.NameEN;

                // Email required (extra safety)
                if (string.IsNullOrWhiteSpace(registerVM.Email))
                {
                    ModelState.AddModelError(nameof(RegisterVM.Email), lang == "ar" ? "البريد مطلوب" : "Email is required");
                }

                #endregion

                if (!ModelState.IsValid)
                    return View(registerVM);

                // Normalize email
                var email = registerVM.Email!.Trim();

                // 4) Generate safe unique username (بدل Random عشان ما يعملش collision)
                var userName = await GenerateUniqueUsernameAsync(email);

                var applicationUser = new ApplicationUser
                {
                    NameEN = registerVM.NameEN,
                    NameAR = registerVM.NameAR,
                    Email = email,
                    UserName = userName
                };

                IdentityResult result;

                if (info != null)
                {
                    // Google → بدون باسورد
                    result = await _userManager.CreateAsync(applicationUser);
                }
                else
                {
                    // عادي
                    result = await _userManager.CreateAsync(applicationUser, registerVM.Password!);
                }

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    TempData["error-notifications"] = lang == "ar"
                        ? "فشل إنشاء الحساب. راجع الأخطاء."
                        : "Registration failed. Please check the errors.";

                    return View(registerVM);
                }

                // Success
                await _userManager.AddToRoleAsync(applicationUser, "Student");

                var std = new Models.Student
                {
                    Id = applicationUser.Id,
                    NameAr = applicationUser.NameAR ?? "",
                    NameEn = applicationUser.NameEN ?? "",
                    CreatedDate = DateTime.UtcNow
                };

                await unitOfWork.StudentRepository.CreateAsync(std);
                await unitOfWork.CommitAsync();

                // Google → Link + SignIn + Redirect
                if (info != null)
                {
                    await _userManager.AddLoginAsync(applicationUser, info);
                    await _signInManager.SignInAsync(applicationUser, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                // Normal registration → Email confirmation
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(applicationUser);

                var confirmationLink = Url.Action(
                    "ConfirmEmail",
                    "Account",
                    new { userId = applicationUser.Id, token },
                    Request.Scheme
                );

                await _emailSender.SendEmailAsync(
                    applicationUser.Email!,
                    lang == "ar" ? "تأكيد الحساب" : "Confirm Your Email",
                    lang == "ar"
                        ? $"برجاء تأكيد حسابك من خلال <a href='{confirmationLink}'>الضغط هنا</a>"
                        : $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>."
                );

                TempData["success-notifications"] = lang == "ar"
                    ? "تم إنشاء الحساب! برجاء مراجعة بريدك الإلكتروني لتفعيله."
                    : "Account Created! Please check your email to confirm.";

                return RedirectToAction(nameof(Login));
            }
            catch (Exception)
            {
                // ما نعرضش تفاصيل Exception للمستخدم (security)
                ModelState.AddModelError(string.Empty,
                    lang == "ar" ? "حدث خطأ غير متوقع. حاول مرة أخرى." : "An unexpected error occurred. Please try again.");

                return View(registerVM);
            }
        }

        // Helper: generate unique username (بدون Random)
        private async Task<string> GenerateUniqueUsernameAsync(string email)
        {
            var baseName = email.Split('@')[0];

            // sanitize
            baseName = new string(baseName.Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '.').ToArray());
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "user";

            // try a few times
            for (int i = 0; i < 5; i++)
            {
                var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
                var candidate = $"{baseName}_{suffix}";

                var exists = await _userManager.FindByNameAsync(candidate);
                if (exists == null)
                    return candidate;
            }

            // fallback
            return $"{baseName}_{Guid.NewGuid():N}";
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
                            return RedirectToAction("index", "Home", new { area = "Instructor" });
                        }
                        else
                        {
                            return RedirectToAction("index", "Home", new { area = "StudentArea" });
                        }
                          
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
           // returnUrl ??= Url.Content("~/");
            returnUrl ??= Url.Action("Index", "Home", new { area = "StudentArea" }) ?? "/StudentArea/Home/Index";


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
                //return LocalRedirect(returnUrl ?? "/");
                return LocalRedirect(returnUrl);

            }

            // لو الحساب جديد، نرسل المستخدم لصفحة GoogleResponse للتحقق
            return RedirectToAction("GoogleResponse", new { returnUrl });
        }


        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string returnUrl = null)
        {
            returnUrl ??= Url.Action("Index", "Home", new { area = "StudentArea" }) ?? "/StudentArea/Home/Index";

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            if (signInResult.Succeeded)
              //  return LocalRedirect(returnUrl ?? "/");
                return LocalRedirect(returnUrl);


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

        public IActionResult AccessDenied(string returnUrl)
        {
            
            string areaName = "";
            if (!string.IsNullOrEmpty(returnUrl))
            {
                var segments = returnUrl.Split('/');
                
                if (segments.Length > 1)
                {
                    areaName = segments[1];
                }
            }

            ViewData["CurrentArea"] = areaName;
            return View();
        }

    }
}
