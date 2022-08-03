using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TechUpdate.Core.Services.News;
using TechUpdate.DataLayer.Entities.Comment;
using TechUpdate.DataLayer.Entities.Operator;
using TechUpdate.Site.InfraStructure;

namespace TechUpdate.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class NewsController : Controller
    {
        private INewsRepository _newsRepository;
        private UserManager<Operator> _userManager;

        public NewsController(INewsRepository newsRepository, UserManager<Operator> userManager)
        {
            _newsRepository = newsRepository;
            _userManager = userManager;
        }

        [Route("News/{newsId}")]
        public async Task<IActionResult> ShowNews(int newsId)
        {
            var page = _newsRepository.getNewsById(newsId);
            if (page != null)
            {
                page.PageVisit += 1;
                _newsRepository.Update(page);
                await _newsRepository.Save();
            }
            return View(page);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveComments(int newsId, string textarea)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            await _newsRepository.AddComment(new Comment()
            {
                NewsId = newsId,
                CommentText = textarea,
                Email = user.Email,
                FullName = user.FirstName + " " + user.LastName,
                CommentState = CommentState.UnConfirm,
                SaveDate = DateTime.Now,
                UserImage = user.Avatar,
            });
            return Redirect("/");
        }
    }
}
