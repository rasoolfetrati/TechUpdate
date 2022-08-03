using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TechUpdate.Core.Services.Groups;
using TechUpdate.Core.Services.News;
using TechUpdate.Core.ViewModels.News;
using TechUpdate.DataLayer.Entities;
using TechUpdate.DataLayer.Entities.Operator;
using TechUpdate.Site.InfraStructure;

namespace TechUpdate.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AutoValidateAntiforgeryToken]
    [Authorize]
    public class NewsController : Controller
    {
        private UserManager<Operator> userManager;
        private INewsRepository _newsRepository;
        private IGroupRepository _groupRepository;
        private IWebHostEnvironment env;

        public NewsController(UserManager<Operator> userManager, INewsRepository newsRepository, IGroupRepository groupRepository, IWebHostEnvironment env)
        {
            this.userManager = userManager;
            _newsRepository = newsRepository;
            _groupRepository = groupRepository;
            this.env = env;
        }
        public IActionResult Index()
        {
            var allnews = _newsRepository.GetAllNews();
            var newslist = new List<NewsViewModel>();
            foreach (var item in allnews)
            {
                newslist.Add(new NewsViewModel()
                {
                    NewsId = item.NewsId,
                    CreateDate = item.CreateDate.ToShamsi(),
                    ImageName = item.ImageName,
                    NewsTitle = item.NewsTitle,
                    PageVisit = item.PageVisit,
                    ShowInSlider = item.ShowInSlider,
                    Writer = item.Writer.FirstName + " " + item.Writer.LastName,
                });
            }
            return View(newslist);
        }

        public IActionResult Add()
        {
            var groups = _groupRepository.GetAllGroups();
            ViewBag.Groups = new SelectList(groups, "GroupID", "GroupTitle");
            return View();
        }

        public async Task<IActionResult> Edit(NewsViewModel model)
        {
            ViewBag.Id = model.NewsId;
            var groups = _groupRepository.GetAllGroups();
            ViewBag.Groups = new SelectList(groups, "GroupID", "GroupTitle");
            var news = await _newsRepository.FindAsync((int)model.NewsId);
            var newsfinal = new NewsViewModel
            {
                NewsId = news.NewsId,
                NewsTitle = news.NewsTitle,
                PageText = news.PageText,
                ShortDescription = news.ShortDescription,
                ShowInSlider = news.ShowInSlider,
                ImageName = news.ImageName,
                GroupID = news.GroupID,
            };

            return View("Add", newsfinal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(NewsViewModel newsView, IFormFile Imagename)
        {
            var user = await userManager.FindByNameAsync(User.Identity.Name);
            var groups = _groupRepository.GetAllGroups();
            ViewBag.Groups = new SelectList(groups, "GroupID", "GroupTitle");

            if (newsView.NewsId == null)
            {
                //Add
                newsView.ImageName = "noimages.png";
                if (Imagename != null && Imagename.IsImage())
                {
                    newsView.ImageName = NameGenerator.GenerateUniqCode().ToString() + Path.GetExtension(Imagename.FileName);
                    var path = Path.Combine(env.WebRootPath, "NewsImage", newsView.ImageName);
                    using (var filestream = new FileStream(path, FileMode.Create))
                    {
                        await Imagename.CopyToAsync(filestream);
                    }
                    ImageConvertor imgResizer = new ImageConvertor();
                    string thumbPath = Path.Combine(env.WebRootPath, "NewsImage/thumb", newsView.ImageName);

                    imgResizer.Image_resize(path, thumbPath, 75);
                }


                await _newsRepository.Add(new News()
                {
                    CreateDate = DateTime.Now,
                    Writer = user,
                    NewsTitle = newsView.NewsTitle,
                    ShortDescription = newsView.ShortDescription,
                    ShowInSlider = newsView.ShowInSlider,
                    PageText = newsView.PageText,
                    PageVisit = 0,
                    GroupID = newsView.GroupID,
                    ImageName = newsView.ImageName,
                });
                await _newsRepository.Save();
                return RedirectToAction("Index");
            }
            else
            {
                //Edit
                if (Imagename != null && Imagename.IsImage())
                {

                    if (newsView.ImageName != "noimages.png")
                    {
                        try
                        {
                            string deleteimagePath = Path.Combine(env.WebRootPath, "NewsImage", newsView.ImageName);
                            if (System.IO.File.Exists(deleteimagePath))
                            {
                                System.IO.File.Delete(deleteimagePath);
                            }

                            string deletethumbPath = Path.Combine(env.WebRootPath, "NewsImage/thumb", newsView.ImageName);
                            if (System.IO.File.Exists(deletethumbPath))
                            {
                                System.IO.File.Delete(deletethumbPath);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                    newsView.ImageName = NameGenerator.GenerateUniqCode() + Path.GetExtension(Imagename.FileName);
                    string imagePath = Path.Combine(env.WebRootPath, "NewsImage", newsView.ImageName);

                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await Imagename.CopyToAsync(stream);
                    }

                    ImageConvertor imgResizer = new ImageConvertor();
                    string thumbPath = Path.Combine(env.WebRootPath, "NewsImage/thumb", newsView.ImageName);

                    imgResizer.Image_resize(imagePath, thumbPath, 75);
                }

                _newsRepository.Update(new News()
                {
                    NewsId = newsView.NewsId.Value,
                    NewsTitle = newsView.NewsTitle,
                    ShortDescription = newsView.ShortDescription,
                    ShowInSlider = newsView.ShowInSlider,
                    PageText = newsView.PageText,
                    GroupID = newsView.GroupID,
                    ImageName = newsView.ImageName,
                });
                await _newsRepository.Save();
                return RedirectToAction("Index");
            }
        }
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(NewsViewModel model)
        {
            var newsimage = await _newsRepository.FindAsync(model.NewsId.Value);

            try
            {
                string deleteimagePath = Path.Combine(env.WebRootPath, "NewsImage", newsimage.ImageName);
                if (System.IO.File.Exists(deleteimagePath))
                {
                    System.IO.File.Delete(deleteimagePath);
                }

                string deletethumbPath = Path.Combine(env.WebRootPath, "NewsImage/thumb", newsimage.ImageName);
                if (System.IO.File.Exists(deletethumbPath))
                {
                    System.IO.File.Delete(deletethumbPath);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            await _newsRepository.DeleteAsync(model.NewsId.Value);
            await _newsRepository.Save();
            return RedirectToAction("Index");
        }
    }
}