using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Epidaurus.Domain;
using Epidaurus.Domain.Entities;
using Epidaurus.ScannerLib;
using Epidaurus.ViewModels;

namespace Epidaurus.Controllers
{
    public class ToWatchController : Controller
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private readonly MovieSystemService _movieSystemService;
        private readonly MovieInformationUpdater _movieInformationUpdater;

        public ToWatchController(MovieSystemService mss, MovieInformationUpdater movieInformationUpdater)
        {
            _movieSystemService = mss;
            _movieInformationUpdater = movieInformationUpdater;
        }

        // GET: /ToWatch/

        [Authorize]
        public ActionResult Index()
        {
            var tmp = _movieSystemService.UsersWithToWatch as List<User>;  //FindIndex not available on IList..
            var idx = tmp.FindIndex(m => m.Username == User.Identity.Name);
            if (idx > 0)
            {
                var t = tmp[idx];
                tmp[idx] = tmp[0];
                tmp[0] = t;
            }

            IList<UserWithToWatch> ret = tmp.Select(el => new UserWithToWatch() { User = el, ToWatches = el.ToWatches.OrderBy(el2 => el2.Movie.Title).ToList() } ).ToList();

            return View(ret);
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddToWatch(string imdbId, string comment)
        {
            try
            {
                var movie = _movieInformationUpdater.GetOrCreateNewMovieWithImdbId(imdbId);
                var toWatch = _movieSystemService.AddToWatch(movie.Id, comment);
                return View("MovieEntry", toWatch);
            }
            catch (Exception ex)
            {
                _log.ErrorException("AddToWatch", ex);
                return Json("error:" + ex.Message);
            }
        }

        [Authorize]
        [HttpPost]
        public JsonResult RemoveToWatch(int movieId)
        {
            try
            {
                _movieSystemService.RemoveToWatch(movieId);
                return Json("success");
            }
            catch (Exception ex)
            {
                _log.ErrorException("RemoveToWatch", ex);
                return Json("ERROR: " + ex.Message);
            }
        }
    }
}
