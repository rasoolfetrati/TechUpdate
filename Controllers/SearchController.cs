using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TechUpdate.Core.Services.News;

namespace TechUpdate.Controllers
{
    public class SearchController : Controller
    {
        private INewsRepository newsRepository;

        public SearchController(INewsRepository newsRepository)
        {
            this.newsRepository = newsRepository;
        }
        [HttpGet]
        public IActionResult Index(string q)
        {
            ViewBag.Name = q;
            var results = newsRepository.SearchNews(q);
            return View(results);
        }
    }
}