using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Epidaurus.Domain.Entities;
using Epidaurus.Security;

namespace Epidaurus.Controllers
{
    [Authorize(Roles="Admin")]
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
        public ActionResult Create([Bind(Exclude = "LastLogin")] User user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var db = EpidaurusDbContainer.Create())
                    {
                        user.LastLogin = new DateTime(2000, 1, 1);
                        if (!string.IsNullOrWhiteSpace(user.Password))
                            user.Password = SecurityService.GetPasswordHash(user.Password);

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

            return View(user);
        }
        
        //
        // GET: /Users/Edit/5
 
        public ActionResult Edit(string id)
        {
            using (var db = EpidaurusDbContainer.Create())
            {
                var user = db.Users.First(u => u.Username == id);
                user.Password = "";
                return View(user);
            }
        }

        //
        // POST: /Users/Edit/5

        [HttpPost]
        public ActionResult Edit(string id, [Bind(Exclude = "LastLogin")] User user)
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
                        oldUser.IsAdmin = user.IsAdmin;
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
