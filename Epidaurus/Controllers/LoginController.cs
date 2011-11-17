using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Epidaurus.Security;
using Epidaurus.ViewModels;
using System.Web.Security;

namespace Epidaurus.Controllers
{
    public class LoginController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("LogOn");
        }

        [HttpGet]
        public ActionResult LogOn(string ReturnUrl)
        {
            if (Security.SecurityService.LoggedInUser == null)
            {
                Security.SecurityService.TryLoginFromCookie(Request.Cookies);
                if (Security.SecurityService.LoggedInUser != null)
                    return RedirectAfterLogOn(ReturnUrl);
            }

            var vm = new LogOnViewModel();
            vm.RememberLogin = true;
            return View(vm);
        }

        [HttpGet]
        public ActionResult Logout()
        {
            Security.SecurityService.Logout(Request.Cookies, Response.Cookies);
            return RedirectToAction("LogOn");
        }

        [HttpPost]
        public ActionResult LogOn(LogOnViewModel lovm, string ReturnUrl)
        {
            if (this.ModelState.IsValid)
            {
                lovm.Username = lovm.Username.Trim();
                lovm.Password = lovm.Password.Trim();
                if (Security.SecurityService.Login(lovm.Username, lovm.Password))
                {
                    if (lovm.RememberLogin)
                        RememberLogin();

                    return RedirectAfterLogOn(ReturnUrl);
                }
                else
                    ModelState.AddModelError("", "Invalid username/password.");
            }

            return View(lovm);
        }

        private ActionResult RedirectAfterLogOn(string ReturnUrl)
        {
            if (!string.IsNullOrWhiteSpace(ReturnUrl))
                return Redirect(ReturnUrl);
            else
                return RedirectToAction("Index", "Movie");
        }

        private void RememberLogin()
        {
            Security.SecurityService.RememberLoggedInUser(Response.Cookies);
        }

        //Get /Users/GenerateHash
        public string GenerateHash(string password)
        {
            return SecurityService.GetPasswordHash(password);
        }
    }
}
