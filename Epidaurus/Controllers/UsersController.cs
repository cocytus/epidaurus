using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Epidaurus.Domain.Entities;
using Epidaurus.Security;
using Epidaurus.ViewModels.UserEditorViewModels;

namespace Epidaurus.Controllers
{
    [Authorize(Roles = EpiRoles.Admin)]
    public class UsersController : Controller
    {
        //
        // GET: /Users/

        public ActionResult Index()
        {
            using (var db = EpidaurusDbContainer.Create())
            {
                var users = db.Users.ToList();
                return View(users);
            }
        }

        //
        // GET: /Users/Create

        public ActionResult Create()
        {
            return View();
        } 

        //
        // POST: /Users/Create

        [HttpPost]
        public ActionResult Create([Bind(Exclude = "LastLogin")] UserViewModel userVm)
        {
            if (string.IsNullOrWhiteSpace(userVm.Password))
                ModelState.AddModelError("Password", "Password not be empty");

            if (ModelState.IsValid)
            {
                try
                {
                    using (var db = EpidaurusDbContainer.Create())
                    {
                        var user = new User()
                        {
                            Username = userVm.Username,
                            Name = userVm.Name,
                            Password = SecurityService.GetPasswordHash(userVm.Password),
                            Roles = userVm.Roles ?? "",
                            LastLogin = new DateTime(2000, 1, 1),
                        };
                           

                        db.AddToUsers(user);
                        db.SaveChanges();
                    }

                    return RedirectToAction("Index");
                }
                catch(Exception ex)
                {
                    ModelState.AddModelError("", "DB Error! " + ex.Message);
                }
            }

            return View(userVm);
        }
        
        //
        // GET: /Users/Edit/5
 
        public ActionResult Edit(string id)
        {
            using (var db = EpidaurusDbContainer.Create())
            {
                var user = db.Users.First(u => u.Username == id);
                var vm = (UserViewModel)user;
                vm.Password = "";
                return View(vm);
            }
        }

        //
        // POST: /Users/Edit/5

        [HttpPost]
        public ActionResult Edit(string id, [Bind(Exclude = "LastLogin")] UserViewModel user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var db = EpidaurusDbContainer.Create())
                    {
                        var oldUser = db.Users.First(u => u.Username == id);
                        if (!string.IsNullOrWhiteSpace(user.Password))
                            oldUser.Password = SecurityService.GetPasswordHash(user.Password);
                        oldUser.Roles = user.Roles ?? "";
                        oldUser.Name = user.Name;
                        db.SaveChanges();
                    }

                    return RedirectToAction("Index");
                }
                catch(Exception ex)
                {
                    ModelState.AddModelError("db", ex);
                }
            }

            return View(user);
        }
    }
}
