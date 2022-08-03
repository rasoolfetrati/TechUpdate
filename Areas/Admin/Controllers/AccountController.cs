using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TechUpdate.Core.Services.User;
using TechUpdate.Core.ViewModels.Users;
using TechUpdate.DataLayer.Entities.Operator;
using TechUpdate.Site.InfraStructure;

namespace TechUpdate.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AutoValidateAntiforgeryToken]
    public class AccountController : Controller
    {
        private UserManager<Operator> userManager;
        private SignInManager<Operator> signInManager;
        private IUserService _userService;

        public AccountController(UserManager<Operator> userManager, SignInManager<Operator> signInManager, IUserService userService)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            _userService = userService;
        }
        [Authorize]
        public IActionResult Index()
        {
            var allUsers = _userService.GetAllUsers();
            var listUsers = new List<UserViewModel>();
            if (allUsers != null)
            {
                foreach (var user in allUsers)
                {
                    listUsers.Add(new UserViewModel()
                    {
                        UserId = user.Id,
                        Username = user.FirstName + " " + user.LastName,
                        Email = user.Email,
                        Website = user.WebSite,
                        IsActive = user.EmailConfirmed,
                        PhoneNumber = user?.PhoneNumber,
                    });
                }
            }
            return View(listUsers);
        }

        #region AddingUser
        [Authorize(Policy = "AdminOnly")]
        public IActionResult AddUser()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddUser(UserAddingViewModel user)
        {
            if (!ModelState.IsValid)
                return View(user);
            if (user.UserId == null)
            {
                var addclint = new Operator()
                {
                    Email = user.Email,
                    Avatar = "deafult.png",
                    UserName = user.Username,
                    EmailConfirmed = true,
                    ActiveCode = NameGenerator.GenerateUniqCode().ToString(),
                };
                var SingUp = await userManager.CreateAsync(addclint, user.Password);
                if (SingUp.Succeeded)
                {
                    if (user.Claim == Claimtype.User)
                    {
                        var claim = await userManager.AddClaimAsync(addclint, new Claim("UserType", "Client"));
                        if (claim.Succeeded)
                        {
                            return RedirectToAction("Index");
                        }
                    }
                    else if (user.Claim == Claimtype.Journal)
                    {
                        var claim = await userManager.AddClaimAsync(addclint, new Claim("Journal", "Operator"));
                        if (claim.Succeeded)
                        {
                            return RedirectToAction("Index");
                        }

                    }
                }
                foreach (var error in SingUp.Errors)
                {
                    ModelState.AddModelError(String.Empty, error.Description);
                }
                return View(user);
            }
            else
            {
                var getuser = await userManager.FindByIdAsync(user.UserId);
                if (user.Claim == Claimtype.User)
                {
                    await userManager.RemoveClaimAsync(getuser, new Claim("Journal", "Operator"));
                    var claim = await userManager.AddClaimAsync(getuser, new Claim("UserType", "Client"));
                    if (claim.Succeeded)
                    {
                        await _userService.UpdateAsync(new Operator()
                        {
                            Id = user.UserId,
                            Email = user.Email,
                            Avatar = "deafult.png",
                            UserName = user.Username,
                            EmailConfirmed = user.IsActive,
                        });
                        return RedirectToAction("Index");
                    }
                }
                else if (user.Claim == Claimtype.Journal)
                {
                    await userManager.RemoveClaimAsync(getuser, new Claim("UserType", "Client"));
                    var claim = await userManager.AddClaimAsync(getuser, new Claim("Journal", "Operator"));
                    if (claim.Succeeded)
                    {
                        await _userService.UpdateAsync(new Operator()
                        {
                            Id = user.UserId,
                            Email = user.Email,
                            Avatar = "deafult.png",
                            UserName = user.Username,
                            EmailConfirmed = user.IsActive,
                        });
                        return RedirectToAction("Index");
                    }
                }
            }

            return BadRequest();
        }
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> EditUser(UserAddingViewModel user)
        {
            var finduser = await userManager.FindByIdAsync(user.UserId);
            var editing = new UserAddingViewModel()
            {
                UserId = finduser.Id,
                Username = finduser.UserName,
                Email = finduser.Email,
                IsActive = finduser.EmailConfirmed,
            };
            return View("AddUser", editing);
        }
        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> DeleteUser(UserAddingViewModel user)
        {
            var finduser = await userManager.FindByIdAsync(user.UserId);
            var getclaim = await userManager.GetClaimsAsync(finduser);
            if (!getclaim.Any(c => c.Value == "Operator"))
            {
                await userManager.DeleteAsync(finduser);
                await _userService.Save();
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }
        #endregion

        #region Filter

        [Authorize]
        [HttpPost]
        public IActionResult Filter(string email, string phone, bool? state)
        {
            var filter = _userService.SearchAsync(email, phone, state);
            return View("Index", filter);
        }

        #endregion

        #region Login
        [HttpGet]

        public IActionResult Signin(string returnUrl = null)
        {
            if (signInManager.IsSignedIn(User))
                return Redirect("/");

            ViewBag.returnurl = returnUrl;
            return PartialView("Signin");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signin(AdminLoginViewModel model, string returnUrl = null)
        {
            if (signInManager.IsSignedIn(User))
                return Redirect("/");
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByNameAsync(model.Username);
                if (user != null)
                {
                    var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
                    if (result.Succeeded)
                    {
                        var claims = await userManager.GetClaimsAsync(user);
                        if (claims.Any(c => c.Value == "Operator" || c.Value == "Journal"))
                        {
                            if (returnUrl != null)
                            {
                                return Redirect(returnUrl);
                            }
                            else
                            {
                                return RedirectToAction("Index", "Home");
                            }
                        }
                        else
                        {
                            await signInManager.SignOutAsync();
                            return Redirect("/AccessDenied");
                        }
                    }
                    ModelState.AddModelError(String.Empty, "رمز عبور یا نام کاربری اشتباه می باشد!");
                }
                ModelState.AddModelError(String.Empty, "چنین کاربری در سیستم موجود نمی باشد!");
                return PartialView(model);
            }

            return PartialView(model);
        }

        #endregion

    }
}