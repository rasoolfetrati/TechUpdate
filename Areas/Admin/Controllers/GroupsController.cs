using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechUpdate.Core.Services.Groups;
using TechUpdate.Core.ViewModels.Groups;
using TechUpdate.DataLayer.Context;
using TechUpdate.DataLayer.Entities;
using TechUpdate.Site.InfraStructure;

namespace TechUpdate.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class GroupsController : Controller
    {
        private readonly TechUpdateContext _context;
        private IGroupRepository _groupRepository;
        private IWebHostEnvironment env;

        public GroupsController(TechUpdateContext context, IGroupRepository groupRepository, IWebHostEnvironment env)
        {
            _context = context;
            _groupRepository = groupRepository;
            this.env = env;
        }


        // GET: Admin/Groups
        public IActionResult Index()
        {
            var groups = _groupRepository.GetAllGroups();
            var groupLists = new List<GroupViewModel>();
            if (groups != null)
            {
                foreach (var item in groups)
                {
                    groupLists.Add(new GroupViewModel()
                    {
                        GroupID = item.GroupID,
                        GroupTitle = item.GroupTitle,
                        ShowInCategory = item.ShowInCategory
                    });
                }
            }
            return View(groupLists);
        }


        // GET: Admin/Groups/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GroupViewModel groups)
        {
            if (ModelState.IsValid)
            {
                groups.Imagename = "noimages.png";
                if (groups.ImageGroup != null && groups.ImageGroup.IsImage())
                {
                    groups.Imagename = NameGenerator.GenerateUniqCode().ToString() + Path.GetExtension(groups.ImageGroup.FileName);
                    var path = Path.Combine(env.WebRootPath, "GroupImages", groups.Imagename);
                    using (var filestream = new FileStream(path, FileMode.Create))
                    {
                        await groups.ImageGroup.CopyToAsync(filestream);
                    }
                }
                await _groupRepository.Add(new Groups()
                {
                    GroupTitle = groups.GroupTitle,
                    ShowInCategory = groups.ShowInCategory,
                    Imagename = groups.Imagename
                });
                await _groupRepository.Save();
                return RedirectToAction(nameof(Index));
            }
            return View(groups);
        }

        // GET: Admin/Groups/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var groups = await _groupRepository.FindAsync(id.Value);
            if (groups == null)
            {
                return NotFound();
            }
            var groupFinal = new GroupViewModel
            {
                GroupID = groups.GroupID,
                GroupTitle = groups.GroupTitle,
                ShowInCategory = groups.ShowInCategory,
                Imagename = groups.Imagename,
            };
            return View(groupFinal);
        }

        // POST: Admin/Groups/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GroupViewModel groups)
        {
            if (id != groups.GroupID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {

                    if (groups.Imagename != "noimages.png")
                    {
                        string deleteimagePath = Path.Combine(env.WebRootPath, "GroupImages", groups.Imagename);
                        if (System.IO.File.Exists(deleteimagePath))
                        {
                            System.IO.File.Delete(deleteimagePath);
                        }
                    }
                    if (groups.ImageGroup != null && groups.ImageGroup.IsImage())
                    {
                        groups.Imagename = NameGenerator.GenerateUniqCode().ToString() + Path.GetExtension(groups.ImageGroup.FileName);
                        var path = Path.Combine(env.WebRootPath, "GroupImages", groups.Imagename);
                        using (var filestream = new FileStream(path, FileMode.Create))
                        {
                            await groups.ImageGroup.CopyToAsync(filestream);
                        }
                    }

                    await _groupRepository.Update(new Groups()
                    {
                        GroupID = groups.GroupID.Value,
                        GroupTitle = groups.GroupTitle,
                        Imagename = groups.Imagename,
                        ShowInCategory = groups.ShowInCategory
                    });
                    await _groupRepository.Save();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GroupsExists(groups.GroupID.Value))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(groups);
        }


        // POST: Admin/Groups/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _groupRepository.DeleteAsync(id);
            await _groupRepository.Save();
            return RedirectToAction(nameof(Index));
        }

        private bool GroupsExists(int id)
        {
            return _context.GroupsTable.Any(e => e.GroupID == id);
        }
    }
}
