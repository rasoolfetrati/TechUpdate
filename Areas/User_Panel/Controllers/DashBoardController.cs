using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TechUpdate.Core.Services.User;
using TechUpdate.Core.ViewModels.Users;
using TechUpdate.DataLayer.Entities.Operator;

namespace TechUpdate.Areas.User_Panel.Controllers
{
    [Area("User_Panel")]
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class DashBoardController : Controller
    {
        private UserManager<Operator> _userManager;
        private IUserService _userService;
        private SignInManager<Operator> _signInManager;
        private IWebHostEnvironment env;

        public DashBoardController(UserManager<Operator> userManager, IUserService userService,
            SignInManager<Operator> signInManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _userService = userService;
            _signInManager = signInManager;
            this.env = env;
        }

        public async Task<IActionResult> Index(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            var user = await _userManager.FindByNameAsync(this.User.Identity.Name);
            return View(user);
        }

        [HttpGet]
        public IActionResult Edit()
        {
            var username = _userService.GetProfileViewModel(User.Identity.Name);

            return View(username);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Operator profile, IFormFile UserAvatar)
        {
            _userService.EditProfile(User.Identity.Name, profile, UserAvatar);
            ViewBag.success = "حساب کاربری شما با موفقیت ویرایش شد!";
            return View();
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {

                var user = await _userManager.FindByNameAsync(User.Identity.Name);
                var res = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash,
                    model.OldPassword);
                if (res != PasswordVerificationResult.Failed)
                {
                    string token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    await _userManager.ResetPasswordAsync(user, token, model.Password);
                    await _userService.Save();
                    ViewBag.Success = "پسورد شما با موفقيت تغيير يافت لطفا مجددا وارد حساب كاربري خود شويد!";
                    return View();
                }
                else
                {
                    ModelState.AddModelError(String.Empty, "رمز عبور فعلی خود را اشتباه وارد کردید!");
                }
            }

            return View();
        }

        public async Task<IActionResult> SingOut()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/");
        }
    }
}