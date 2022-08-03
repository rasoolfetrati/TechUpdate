using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SendEmail;
using TechUpdate.Core.Services.Comment;
using TechUpdate.Core.Services.ContactUs;
using TechUpdate.Core.Services.News;
using TechUpdate.Core.ViewModels.Comments;
using TechUpdate.Core.ViewModels.Contact;
using TechUpdate.DataLayer.Entities.Comment;
using TechUpdate.DataLayer.Entities.Operator;
using TechUpdate.Site.InfraStructure;

namespace TechUpdate.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class HomeController : Controller
    {
        private IContactRepository _contactRepository;
        private UserManager<Operator> _userManager;
        private IViewRenderService _renderService;
        private ICommentRepository _commentRepository;

        public HomeController(IContactRepository contactRepository, UserManager<Operator> userManager, IViewRenderService renderService, ICommentRepository commentRepository)
        {
            _contactRepository = contactRepository;
            _userManager = userManager;
            _renderService = renderService;
            _commentRepository = commentRepository;
        }
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            ViewBag.fullname = user.FirstName + " " + user.LastName;
            ViewBag.date = DateTime.Now.ToShamsi();
            return View();
        }
        [HttpGet]
        public IActionResult ViewContactUs()
        {
            var cnt = _contactRepository.GetAllContacts();
            if (cnt.Count != 0)
            {
                var cntlist = new List<ContactViewModel>();
                foreach (var item in cnt)
                {
                    cntlist.Add(new ContactViewModel()
                    {
                        Id = item.Id,
                        Fullname = item.Fullname,
                        Email = item.Email,
                        WebSite = item.WebSite,
                        Message = item.Message,
                        CreateData = item.CreateData.ToShamsi(),
                    });
                }
                return View(cntlist);
            }
            else
            {
                ViewBag.Empty = "در حال حاضر پیام جدیدی ارسال نشده است!";
                return View();
            }
        }
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteMessage(ContactViewModel model)
        {
            await _contactRepository.Delete(model.Id);
            await _contactRepository.Save();
            return RedirectToAction("ViewContactUs");
        }
        [HttpGet]
        public IActionResult SendMail(string email)
        {
            ViewBag.getMail = email;
            return View();
        }
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult SendMail(RespondViewModel model)
        {
            var respond = new RespondViewModel()
            {
                UserName = model.Email,
                Email = model.Email,
                Message = model.Message,
            };
            string body = _renderService.RenderToStringAsync("_RespondEmail", respond);

            Site.InfraStructure.SendEmail.Send(model.Email, "پاسخ", body);
            ViewBag.Success = true;
            return View();
        }

        public IActionResult ManageComments()
        {
            var cms = _commentRepository.GetComments();
            var cmslist = new List<CommentViewModel>();
            foreach (var comment in cms)
            {
                cmslist.Add(new CommentViewModel()
                {
                    NewsId = comment.NewsId,
                    CommentID = comment.CommentID,
                    CommentState = CommentState.UnConfirm,
                    FullName = comment.FullName,
                    CommentText = comment.CommentText,
                    Email = comment.Email,
                    SaveDate = comment.SaveDate,
                });
            }
            return View(cmslist);
        }
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ConfirmComment(CommentViewModel cmViewModel)
        {
            var comment = await _commentRepository.FindComment(cmViewModel.CommentID);
            await _commentRepository.Update(comment);
            return View("ManageComments");
        }
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteComment(CommentViewModel cmViewModel)
        {
           await _commentRepository.DeleteComment(cmViewModel.CommentID);
           return RedirectToAction("ManageComments");
        }
    }
}