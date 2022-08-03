using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using reCAPTCHA.AspNetCore;
using SendEmail;
using TechUpdate.Core.Services.User;
using TechUpdate.Core.ViewModels.Users;
using TechUpdate.DataLayer.Entities.Operator;
using TechUpdate.Site.InfraStructure;
using TopLearn.Core.Convertors;
using Claim = System.Security.Claims.Claim;


namespace TechUpdate.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class AccountController : Controller
    {
        private UserManager<Operator> _userManager;
        private SignInManager<Operator> _signInManager;
        private IUserService _userService;
        private IViewRenderService _renderService;
        private IRecaptchaService _recaptchaService;

        public AccountController(UserManager<Operator> userManager, SignInManager<Operator> signInManager, IUserService userService, IViewRenderService renderService, IRecaptchaService recaptchaService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userService = userService;
            _renderService = renderService;
            _recaptchaService = recaptchaService;
        }
        #region Login

        public async Task<IActionResult> Login(string returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");
            var model = new LoginViewModel()
            {
                returnUrl = returnUrl,
                ExternalLogin = await _signInManager.GetExternalAuthenticationSchemesAsync()
            };
            ViewBag.ReturnUrl = returnUrl;
            return PartialView("Login", model);
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");
            model.returnUrl = returnUrl;
            model.ExternalLogin = await _signInManager.GetExternalAuthenticationSchemesAsync();
            ViewBag.ReturnUrl = returnUrl;

            if (ModelState.IsValid)
            {
                var res =await _recaptchaService.Validate(Request);
                if (!res.success)
                {
                    ModelState.AddModelError(String.Empty, "اعتبار سنجی captcha نا موفق بود!");
                    return PartialView("Login", model);
                }
                else
                {
                    var user = await _userManager.FindByEmailAsync(FixedText.FixEmail(model.Email));
                    if (user != null)
                    {
                        var signin = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);

                        if (signin.Succeeded && !signin.IsNotAllowed)
                        {
                            var claimCheck = await _userManager.GetClaimsAsync(user);
                            if (claimCheck.Any(c => c.Value == "Client"))
                            {
                                return RedirectToAction("Index", "DashBoard", new { area = "User_Panel" });
                            }
                        }
                        else
                        {
                            ViewBag.Error = "رمز عبور یا نام کاربری خود را اشتباه وارد کرده اید یا حساب کاربری خود را فعال نکرده اید!";
                            return PartialView("Login", model);
                        }
                    }
                    else
                    {
                        ViewBag.Error = "كاربري با اين مشخصات يافت نشد!";
                        return PartialView("Login", model);
                    }

                }

            }
            ModelState.AddModelError(String.Empty, "لطفا تمام مقادیر را پر نمایید");
            return PartialView("Login", model);
        }
        [HttpPost]
        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/");
        }
        #endregion

        #region External

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl)
        {
            var redirectUrl = Url.Action("ExternalLoginCallBack", "Account",
                new { ReturnUrl = returnUrl });

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }


        public async Task<IActionResult> ExternalLoginCallBack(string returnUrl = null, string remoteError = null)
        {
            returnUrl =
                (returnUrl != null && Url.IsLocalUrl(returnUrl)) ? returnUrl : Url.Content("~/");

            var loginViewModel = new LoginViewModel()
            {
                returnUrl = returnUrl,
                ExternalLogin = await _signInManager.GetExternalAuthenticationSchemesAsync()
            };

            if (remoteError != null)
            {
                ModelState.AddModelError("", $"Error : {remoteError}");
                return View("Login", loginViewModel);
            }

            var externalLoginInfo = await _signInManager.GetExternalLoginInfoAsync();
            if (externalLoginInfo == null)
            {
                ModelState.AddModelError("ErrorLoadingExternalLoginInfo", $"مشکلی پیش آمد");
                return View("Login", loginViewModel);
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(externalLoginInfo.LoginProvider,
                externalLoginInfo.ProviderKey, false, true);

            if (signInResult.Succeeded)
            {
                return Redirect(returnUrl);
            }

            var email = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email);

            if (email != null)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    var userName = email.Split('@')[0];
                    user = new Operator()
                    {
                        UserName = (userName.Length <= 10 ? userName : userName.Substring(0, 10)),
                        Email = email,
                        EmailConfirmed = true,
                        Avatar = "deafult.png",
                    };

                    await _userManager.CreateAsync(user);
                }

                await _userManager.AddLoginAsync(user, externalLoginInfo);
                await _signInManager.SignInAsync(user, false);

                return Redirect(returnUrl);
            }

            ViewBag.ErrorTitle = "لطفا با بخش پشتیبانی تماس بگیرید";
            ViewBag.ErrorMessage = $"دریافت کرد {externalLoginInfo.LoginProvider} نمیتوان اطلاعاتی از";
            return RedirectToAction("ContactUs", "Home");
        }

        #endregion

        #region Register
        [HttpGet]
        public IActionResult Register()
        {
            return PartialView("Register");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel register)
        {
            if (ModelState.IsValid)
            {
                var rec = await _recaptchaService.Validate(Request);
                if (!rec.success)
                {
                    ModelState.AddModelError(String.Empty, "لطفا من ربات نیستم را تیک بزنید!");
                    return PartialView("Register");
                }
                else
                {
                    var client = new Operator()
                    {
                        ActiveCode = NameGenerator.GenerateUniqCode(),
                        Email = FixedText.FixEmail(register.Email),
                        UserName = FixedText.FixEmail(register.Email),
                        EmailConfirmed = false,
                        Avatar = "deafult.png",
                    };

                    var singup = await _userManager.CreateAsync(client, register.Password);
                    if (singup.Succeeded)
                    {
                        var claim = await _userManager.AddClaimAsync(client, new Claim("UserType", "Client"));
                        if (claim.Succeeded)
                        {
                            string body = _renderService.RenderToStringAsync("_ActiveEmail", client);

                            Site.InfraStructure.SendEmail.Send(client.Email, "فعال سازی", body);
                            ViewBag.Success = true;
                            return View("Success");
                        }
                    }
                    foreach (var error in singup.Errors)
                    {
                        ModelState.AddModelError(String.Empty, error.Description);
                    }
                }
            }
            return PartialView("Register", register);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IsEmailInUse(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Json(true);
            return Json("ایمیل وارد شده از قبل موجود است");
        }

        #endregion

        #region Active Account

        public IActionResult ActiveAccount(string id)
        {
            ViewBag.IsActive = _userService.ActiveAccount(id);
            return View();
        }

        #endregion

        #region ForgetPassword
        [HttpGet]
        public IActionResult ForgetPassword()
        {
            return PartialView();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgetPassword(string email)
        {
            if (!string.IsNullOrEmpty(email))
            {
                string getemail = FixedText.FixEmail(email);
                Operator user = _userService.GetUserByEmail(getemail);
                if (user != null)
                {
                    string bodyEmail = _renderService.RenderToStringAsync("_ForgetPassword", user);

                    Site.InfraStructure.SendEmail.Send(user.Email, "بازيابي رمز عبور", bodyEmail);
                    ViewBag.Success = true;
                    return PartialView("ForgetPassword");
                }
                else
                {
                    ViewBag.Error = "كاربري با مشخصات وارد شده يافت نشد!";
                    return PartialView();
                }
            }
            else
            {
                ViewBag.Error = "لطفا ايميل خود را وارد كنيد";
                return PartialView();
            }
        }

        #endregion

        #region ResetPassword
        public IActionResult ResetPassword(string id)
        {
            var user = new Operator()
            {
                ActiveCode = id
            };
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string newpassword, string repassword, string activeCode)
        {
            if (!newpassword.Equals(repassword))
            {
                ViewBag.Error = "رمز عبور با تكرار آن مطابقت ندارد!";
                return View();
            }
            else if (string.IsNullOrEmpty(newpassword))
            {
                ViewBag.Error = "رمز عبور جديد خود را وارد كنيد!";
                return View();
            }
            var user = _userService.GetUserByActiveCode(activeCode);
            if (user == null)
            {
                return NotFound();
            }
            else
            {
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                }
                string token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, newpassword);
                user.ActiveCode = NameGenerator.GenerateUniqCode().ToString();
                await _userService.Save();
                return RedirectToAction("Login", "Account");
            }
        }

        #endregion
        [Route("AccessDenied")]
        public IActionResult AccessDenied()
        {
            return PartialView();
        }
    }
}