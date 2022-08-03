using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TechUpdate.Core.Services.ContactUs;
using TechUpdate.Core.Services.News;
using TechUpdate.Core.ViewModels.Contact;
using TechUpdate.Core.ViewModels.News;
using TechUpdate.DataLayer.Entities;
using TechUpdate.Site.InfraStructure;

namespace TechUpdate.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class HomeController : Controller
    {
        private INewsRepository _newsRepository;
        private IContactRepository _contactRepository;

        public HomeController(INewsRepository newsRepository, IContactRepository contactRepository)
        {
            _newsRepository = newsRepository;
            _contactRepository = contactRepository;
        }
        [HttpGet]
        public IActionResult Index()
        {
            var news = _newsRepository.GetAllNews();
            var list = new List<NewsViewModel>();
            foreach (var item in news)
            {
                list.Add(new NewsViewModel()
                {
                    NewsId = item.NewsId,
                    GroupID = item.GroupID,
                    CreateDate = item.CreateDate.ToShamsi(),
                    ImageName = item.ImageName,
                    Writer = item.Writer.FirstName + " " + item.Writer.LastName,
                    NewsTitle = item.NewsTitle,
                    ShortDescription = item.ShortDescription
                });
            }
            return View(list);
        }
        public IActionResult AboutUs()
        {
            return View();
        }
        [HttpGet]
        public IActionResult ContactUs()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ContactUs(ContactViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            await _contactRepository.Add(new ContactUs()
            {
                CreateData = DateTime.Now,
                Email = model.Email,
                Fullname = model.Fullname,
                Message = model.Message,
                WebSite = model.WebSite
            });
            await _contactRepository.Save();
            return Redirect("/");
        }
    }
}