using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using Epidaurus.Domain.Entities;
using System.Text.RegularExpressions;
using System.Web.Security;

namespace Epidaurus.Security
{
    //Note: Nothing done to prevent stealing of sessions'n stuff.
    public static class SecurityService
    {
        private static SHA1Managed _sha1 = new SHA1Managed();
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();


        public static bool Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            using (var db = EpidaurusDbContainer.Create())
            {
                var passHashed = GetPasswordHash(password);
                var user = db.Users.FirstOrDefault(u => u.Username == username && u.Password == passHashed);
                if (user == null)
                    return false;

                LoginUser(db, user);
                return true;
            }
        }

        private static void LoginUser(EpidaurusDbContainer db, User user)
        {
            user.LastLogin = DateTime.Now;
            db.SaveChanges();

            var roles = user.Roles.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            var principal = new EpiPrincipal(user.Username, roles);
            SetCurrentPrincipal(principal);
            _log.Trace("Loginuser: User: {0} Roles: {1}", user.Username, user.Roles);
        }

        #region Persistent login cookie stuff
        private const string CookieName = "epidaurusRemember";

        //Unsafe, stores guid directly in db, has same effect as storing password in cookie.
        public static void RememberLoggedInUser(HttpCookieCollection responseCookies)
        {
            using (var db = EpidaurusDbContainer.Create())
            {
                var user = LoggedInUser;
                string randGuid = Guid.NewGuid().ToString();

                string cookieValue = string.Format("{0}_{1}", user.Identity.Name, randGuid);
                var cookie = new HttpCookie(CookieName, cookieValue);
                cookie.Expires = DateTime.Now + TimeSpan.FromDays(100);
                cookie.Path = HttpContext.Current.Request.ApplicationPath;
                responseCookies.Add(cookie);

                var rs = new RememberedSessions()
                {
                    UserUsername = user.Identity.Name,
                    GuidHash = randGuid,
                    CreatedAt = DateTime.Now,
                };
                db.AddToRememberedSessions(rs);
                db.SaveChanges();
            }
        }

        public static void TryLoginFromCookie(HttpCookieCollection requestCookies)
        {
            using (var db = EpidaurusDbContainer.Create())
            {
                var session = TryGetRememberedSession(db, requestCookies);
                if (session != null)
                    LoginUser(db, session.User);
            }
        }

        private static RememberedSessions TryGetRememberedSession(EpidaurusDbContainer db, HttpCookieCollection requestCookies)
        {
            var cookie = requestCookies.Get(CookieName);
            if (cookie == null || string.IsNullOrEmpty(cookie.Value))
                return null;
            var match = Regex.Match(cookie.Value, @"^(.+)_([0-9a-f-]+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success)
                return null;
            string userName = match.Groups[1].Value;
            Guid guid;
            if (!Guid.TryParse(match.Groups[2].Value, out guid))
                return null;
            string guidStr = guid.ToString();
            return db.RememberedSessions.FirstOrDefault(el => el.User.Username == userName && el.GuidHash == guidStr);
        }

        public static void ForgetLoggedInUser(HttpCookieCollection requestCookies, HttpCookieCollection responseCookies)
        {
            DeleteRememberedSession(requestCookies);

            var cookie = responseCookies.Get(CookieName);
            cookie.Path = HttpContext.Current.Request.ApplicationPath;
            cookie.Expires = DateTime.Now;
            cookie.Value = "";
            responseCookies.Set(cookie);
        }

        private static void DeleteRememberedSession(HttpCookieCollection requestCookies)
        {
            using (var db = EpidaurusDbContainer.Create())
            {
                var rs = TryGetRememberedSession(db, requestCookies);
                if (rs != null)
                {
                    db.DeleteObject(rs);
                    db.SaveChanges();
                }
            }
        }
        #endregion

        //FIXME unseeded.
        public static string GetPasswordHash(string password)
        {
            var passHash = _sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
            return string.Join("", from el in passHash select el.ToString("X"));
        }

        public static void Logout(HttpCookieCollection requestCookies, HttpCookieCollection responseCookies)
        {
            FormsAuthentication.SignOut();
            Thread.CurrentPrincipal = null;
            HttpContext.Current.Session.Remove("EpiPrincipal");
            ForgetLoggedInUser(requestCookies, responseCookies);
        }

        private static void SetCurrentPrincipal(EpiPrincipal principal)
        {
            FormsAuthentication.SetAuthCookie(principal.Identity.Name, true);

            var ticket = new FormsAuthenticationTicket(1,
              principal.Identity.Name,
              DateTime.Now,
              DateTime.Now.AddMinutes(120),
              false,
              principal.RolesList,
              FormsAuthentication.FormsCookiePath);

            string encTicket = FormsAuthentication.Encrypt(ticket);

            HttpContext.Current.Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, encTicket));
            HttpContext.Current.User = principal;
            Thread.CurrentPrincipal = principal;
        }

        public static void TryLoadCurrentPrincipalFromCookies()
        {
            var authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                var authTicket = FormsAuthentication.Decrypt(authCookie.Value);
                string[] roles = authTicket.UserData.Split(new Char[] { ',' });
                EpiPrincipal userPrincipal = new EpiPrincipal(authTicket.Name, roles);
                HttpContext.Current.User = userPrincipal;
                Thread.CurrentPrincipal = userPrincipal;
            }
        }

        public static EpiPrincipal LoggedInUser
        {
            get
            {
                return HttpContext.Current.User as EpiPrincipal;
            }
        }
    }
}