using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using System.Configuration;
using Epidaurus.Domain;
using Epidaurus.Domain.Entities;
using Epidaurus.ScannerLib;
using Epidaurus.ScannerLib.Imdb;
using Epidaurus.ScannerLib.Utils;
using sysIO=System.IO;
using System.Net;
using System.Linq.Expressions;
using Epidaurus.ViewModels;
using System.Diagnostics;
using Epidaurus.Security;
using System.IO;
using Epidaurus.ScannerLib.Tmdb;
using System.Data.Objects;
using Epidaurus.Domain.Mime;

namespace Epidaurus.Controllers
{
    public class MovieController : Controller
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private readonly MovieSystemService _movieSystemService;
        private readonly MovieInformationUpdater _movieInformationUpdater;
        private readonly IMimeTypeResolver _mimeTypeResolver;

        public MovieController(MovieSystemService movieSystemService, MovieInformationUpdater movieInformationUpdater, IMimeTypeResolver mimeTypeResolver)
        {
            _movieSystemService = movieSystemService;
            _movieInformationUpdater = movieInformationUpdater;
            _mimeTypeResolver = mimeTypeResolver;
        }

        // GET: /Movie/
        [Authorize]
        public ActionResult Index(MovieFilterViewModel mi)
        {
            var db = _movieSystemService.DbEntities;
            //db.ObjectStateManager.
            var vm = CreateMovieListViewModel(mi);
            var movies = MoviesWithReferences;
            ((ObjectQuery<Movie>)movies).MergeOption = MergeOption.NoTracking;

            movies = movies.Where(m => m.MovieAtStorages.Any(mas => !mas.Ignore));

            if (!string.IsNullOrEmpty(mi.Search))
                movies = movies.Where(e => e.Title.Contains(mi.Search) || e.MovieAtStorages.Any(mas=>mas.CleanedName.Contains(mi.Search)));

            if (mi.Person.HasValue)
                movies = movies.Where(m => m.Casts.Any(c => c.PersonId == mi.Person.Value));
            if (mi.Genre.HasValue)
                movies = movies.Where(e => e.Genres.Any(d => d.Id == mi.Genre.Value));

            switch (mi.SortBy.ToLowerInvariant())
            {
                case "score": movies = movies.OrderByDescending(el => el.Score).ThenBy(el=>el.Title); break;
                case "year": movies = movies.OrderByDescending(el => el.Year).ThenBy(el => el.Title); break;
                case "runtime": movies = movies.OrderBy(el => el.Runtime).ThenBy(el => el.Title); break;
                case "title": movies = movies.OrderBy(el => el.Title); break;
                case "addedat": 
                default: 
                    movies = movies.OrderByDescending(el => el.AddedAt).ThenBy(el => el.Title); break;
            }

            var sw = Stopwatch.StartNew();
            
            List<Movie> movieList = movies.ToList();

            ViewBag.DbCallTime = sw.ElapsedMilliseconds;

            if (mi.SelectedUsers != null && mi.SelectedUsers.Length > 0)
            {
                if (mi.SeenNotSeen == SeenNotSeen.NotSeen)
                {
                    var idFilter = new HashSet<int>(from user in db.Users from ss in user.SeenStatuses where mi.SelectedUsers.Contains(user.Username) select ss.Movie.Id);
                    movieList = movieList.Where(m => !idFilter.Contains(m.Id)).ToList();
                }
                else
                {
                    int selUsersCount = mi.SelectedUsers.Length;
                    var idFilter =
                        (from ug in
                                from user in db.Users from ss in user.SeenStatuses where mi.SelectedUsers.Contains(user.Username) select new { MovieId = ss.Movie.Id, Username = user.Username }
                            group ug by ug.MovieId into grp
                            where grp.Count() == selUsersCount
                            select grp.Key).ToList();
                    movieList = movieList.Where(m => idFilter.Contains(m.Id)).ToList();
                }
            }

            vm.Movies = movieList;
            var totalPlayTime = vm.Movies.Sum(el => el.Runtime);
            vm.TotalPlayTime = totalPlayTime.HasValue ? totalPlayTime.Value : 0;

            return View(vm);
        }

        [Authorize]
        public JsonResult ChangeSeenStatus(int id, bool seen)
        {
            try
            {
                _movieSystemService.GetMovieById(id).SetMySeenStatus(seen);
                return new JsonResult() { Data = "OK" };
            }
            catch (Exception ex)
            {
                _log.ErrorException("ChangeSeenStatus", ex);
                return new JsonResult { Data = "ERROR: " + ex.Message };
            }
        }

        private UserViewModel TryGetCurrentUser
        {
            get { return (UserViewModel) _movieSystemService.LoggedInUser; }
        }

        private IQueryable<Movie> MoviesWithReferences
        {
            get 
            { 
                return _movieSystemService.DbEntities.Movies
                    .Include("MovieAtStorages.StorageLocation")
                    .Include("Genres")
                    .Include("Casts.Person")
                    .AsQueryable(); 
            }
        }

        private MovieListViewModel CreateMovieListViewModel(MovieFilterViewModel mi)
        {
            var vm = new MovieListViewModel();
            vm.MovieFilter = mi ?? new MovieFilterViewModel();
            vm.CurrentUser = TryGetCurrentUser;
            vm.WhoHasSeenMovie = GetWhoHasSeenList();

            //Sort stuff
            if (string.IsNullOrEmpty(mi.SortBy))
                mi.SortBy = "addedat";

            var sli = SortSelectListItems.FirstOrDefault(el => el.Value == mi.SortBy);
            if (sli != null)
                sli.Selected = true;
            vm.SortByList = SortSelectListItems;
            //...

            vm.GenreList = GetGenreList(mi.Genre.HasValue ? mi.Genre.Value : -1);
            vm.PersonList = GetPersonList(mi.Person.HasValue ? mi.Person.Value : -1);

            vm.Users = _movieSystemService.DbEntities.Users.ToList().Select(el => (UserViewModel)el).ToList();
            return vm;
        }

        private static readonly List<SelectListItem> SortSelectListItems = 
            new List<SelectListItem>()
            {
                new SelectListItem() {Value = "title", Text = "Tittel"},
                new SelectListItem() {Value = "score", Text = "Score"},
                new SelectListItem() {Value = "year", Text = "År"},
                new SelectListItem() {Value = "runtime", Text = "Lengde"},
                new SelectListItem() {Value = "addedat", Text = "Lagt til"}
            };

        private Dictionary<int, List<string>> GetWhoHasSeenList()
        {
            var list = from el in
                           from ss in _movieSystemService.DbEntities.SeenStatuses
                           select new { MId = ss.Movie.Id, Username = ss.User.Name }
                       group el by el.MId into grp
                       select new { MovieId = grp.Key, SeenBy = grp.Select(el2=>el2.Username) };
            return list.ToDictionary(el => el.MovieId, el => el.SeenBy.ToList());
        }

        private static List<SelectListItem> _people;
        private static readonly object _peopleSync = new object();

        private List<SelectListItem> GetPersonList(int selectedItemId)
        {
            lock (_peopleSync)
            {
                if (_people == null)
                {
                    var emptyFirstItem = new List<SelectListItem>() { new SelectListItem() { Value = "", Text = "[ALLE]" } };
                    var dcount = from el in _movieSystemService.DbEntities.People 
                                 orderby el.Name 
                                 select new { Person = el, Count = el.Casts.Count() };
                    _people = emptyFirstItem.Union(from el in dcount.ToList()
                                                   select new SelectListItem()
                                                   {
                                                       Value = el.Person.Id.ToString(),
                                                       Text = string.Format("{0} ({1})", el.Person.Name, el.Count),
                                                   }).ToList();
                }
            }

            string selectedItemIdStr = selectedItemId.ToString();
            return new List<SelectListItem>(from el in _people select new SelectListItem() { Text = el.Text, Value = el.Value, Selected = el.Value == selectedItemIdStr });
        }

        private static List<SelectListItem> _genres;
        private static readonly object _genresSync = new object();
        private List<SelectListItem> GetGenreList(int selectedItemId)
        {
            lock (_genresSync)
            {
                if (_genres == null)
                {
                    var emptyFirstItem = new List<SelectListItem>() { new SelectListItem() { Value = "", Text = "[ALLE]" } };
                    var gcount = from el in _movieSystemService.DbEntities.Genres orderby el.Name select new { Genre = el, Count = el.Movies.Count() };
                    _genres = emptyFirstItem.Union(from el in gcount.ToList()
                               select new SelectListItem()
                               {
                                   Value = el.Genre.Id.ToString(),
                                   Text = string.Format("{0} ({1})", el.Genre.Name, el.Count),
                               }).ToList();
                }
            }

            string selectedItemIdStr = selectedItemId.ToString();
            return new List<SelectListItem>(from el in _genres select new SelectListItem() { Text = el.Text, Value = el.Value, Selected = el.Value == selectedItemIdStr });
        }

        [ChildActionOnly]
        [Authorize]
        public ActionResult MovieListEntry(int id)
        {
            return View(_movieSystemService.GetMovieById(id));
        }

        [Authorize]
        public PartialViewResult GetImdbSearchResults(string q)
        {
            var query = q + " site:imdb.com";
            var results = GoogleApi.Search(query).Where(el => ImdbApi.IsValidImdbTitleUrl(el.Url)).ToArray();
            foreach (var result in results)
                result.Url = result.Url.Replace("www.", "m.");
            return PartialView(results);
        }

        [Authorize]
        public PartialViewResult TmdbSearch(string q)
        {
            var epiTmdbId = new EpiTmdbApi();
            var results = epiTmdbId.Search(q);

            return PartialView(results);
        }

        [HttpPost]
        [Authorize(Roles = EpiRoles.Admin)]
        public ActionResult UpdateMovie(int id)
        {
            try
            {
                var movie = _movieSystemService.GetMovieById(id);
                _movieInformationUpdater.UpdateMovieFromDataSource(movie);
                _movieSystemService.Save();
                ReloadSafeFileNameCache();
                return View("MovieListEntry", movie);
            }
            catch (Exception ex)
            {
                _log.ErrorException(string.Format("UpdateMovie failed: id: {0} ", id), ex);
                return this.Content(string.Format("<div>ERROR UpdateMovie failed: id: {0} <br/>Exception:<br/>{1}</div>", id, ex.ToString()));
            }
        }

        [HttpPost]
        [Authorize(Roles = EpiRoles.Admin)]
        public ActionResult SetMovieImdbId(int id, string imdbId)
        {
            try
            {
                imdbId = MovieInformationUpdater.CleanImdbId(imdbId);
                var movie = _movieSystemService.SetImdbIdOnMovie(id, imdbId);
                _movieInformationUpdater.UpdateMovieFromDataSource(movie);
                _movieSystemService.Save();
                ReloadSafeFileNameCache();
                return View("MovieListEntry", movie);
            }
            catch (Exception ex)
            {
                _log.ErrorException(string.Format("SetMovieImdbId failed: id: {0} imdbId: {1}", id, imdbId), ex);
                return this.Content(string.Format("<div>ERROR SetMovieImdbId failed: id: {0} imdbId: {1} <br/>Exception:<br/>{2}</div>", id, imdbId, ex.ToString()));
            }
        }

        [HttpPost]
        [Authorize(Roles = EpiRoles.Admin)]
        public ActionResult SetMovieTmdbId(int id, int tmdbId)
        {
            try
            {
                var movie = _movieSystemService.SetTmdbIdOnMovie(id, tmdbId);
                _movieInformationUpdater.UpdateMovieFromDataSource(movie);
                _movieSystemService.Save();
                ReloadSafeFileNameCache();
                return View("MovieListEntry", movie);
            }
            catch (Exception ex)
            {
                _log.ErrorException(string.Format("SetMovieTmdbId failed: id: {0} tmdbId: {1}", id, tmdbId), ex);
                return this.Content(string.Format("<div>ERROR SetMovieTmdbId failed: id: {0} tmdbId: {1} <br/>Exception:<br/>{2}</div>", id, tmdbId, ex.ToString()));
            }
        }
         
        [HttpPost]
        [Authorize(Roles = EpiRoles.Admin)]
        public JsonResult IgnoreSource(int id)
        {
            _movieSystemService.IgnoreMovieSource(id);
            return new JsonResult() { Data = "OK" };
        }

        public ActionResult DownloadSample(int id)
        {
            try
            {
                var movie = _movieSystemService.GetMovieById(id);
                var samplePaths = (from mas in movie.MovieAtStorages 
                                   where mas.StorageLocation.Type=="Folder" && mas.SampleRelativePath != null 
                                   select Path.Combine(mas.StorageLocation.Data1, mas.SampleRelativePath.TrimStart('\\'))).ToList();
                if (samplePaths.Count == 0)
                    throw new InvalidOperationException("Movie does not have a sample");
                var samplePath = (from sp in samplePaths where System.IO.File.Exists(sp) select sp).FirstOrDefault();
                if (samplePath == null)
                    throw new InvalidOperationException("Movie has samples, but none accessible");
                var ext = System.IO.Path.GetExtension(samplePath);
                return this.File(samplePath, _mimeTypeResolver.Resolve(samplePath), movie.Title + " sample" + ext);
            }
            catch (Exception ex)
            {
                throw new HttpException(404, "Not found: " + ex.Message);
            }
        }

        public ActionResult DownloadMovie(int id, int? idx)
        {
            try
            {
                var movie = _movieSystemService.GetMovieById(id);
                var mas = movie.MovieAtStorages.ToList();
                if (mas.Count > 1)
                {
                    if (idx != null)
                        return DownloadMovie(mas[idx.Value]);
                    return View("MoviePathSelect", mas);
                }
                else
                    return DownloadMovie(mas[0]);
            }
            catch (Exception ex)
            {
                throw new HttpException(404, "Not found: " + ex.Message);
            }
        }

        private ActionResult DownloadMovie(MovieAtStorage mas)
        {
            if (string.IsNullOrEmpty(mas.StorageLocation.Rebase))
                throw new InvalidOperationException("No rebase location");

            return Redirect(mas.StorageLocation.Rebase + mas.RelativePath.Replace("\\", "/"));
        }

        #region Poster
        #region NameWithImageUrlCache
        private struct NameWithImageUrl
        {
            public NameWithImageUrl(string name, string imageUrl)
            {
                Name = name;
                ImageUrl = imageUrl;
            }

            public readonly string Name;
            public readonly string ImageUrl;

            public bool IsDefault
            {
                get { return Name == null; }
            }
        }

        private static Dictionary<string, NameWithImageUrl> _safeFileNameCache;
        private static readonly object _safeFileNameCacheLock = new object();
        private static DateTime _cacheTimesOutAt;

        private Dictionary<string, NameWithImageUrl> SafeFileNameCache
        {
            get 
            {
                lock (_safeFileNameCacheLock)
                {
                    if (_safeFileNameCache == null || _cacheTimesOutAt < DateTime.Now)
                        ReloadSafeFileNameCache();

                    return _safeFileNameCache;
                }
            }
        }

        private void ReloadSafeFileNameCache()
        {
            lock (_safeFileNameCacheLock)
            {
                _safeFileNameCache = new Dictionary<string, NameWithImageUrl>();
                foreach (var movie in _movieSystemService.DbEntities.Movies.Where(el => el.ImdbId != null))
                    _safeFileNameCache.Add(movie.ImdbId, new NameWithImageUrl(SafeMovieFileName(movie), movie.ImageUrl));
                _cacheTimesOutAt = DateTime.Now + TimeSpan.FromMinutes(15);
            }
        }

        private NameWithImageUrl GetNameAndImageUrlForMovie(string imdbId)
        {
            NameWithImageUrl tmp;
            if (SafeFileNameCache.TryGetValue(imdbId, out tmp))
                return tmp;
            
            ReloadSafeFileNameCache();
            SafeFileNameCache.TryGetValue(imdbId, out tmp);
            return tmp;
        }
        #endregion

        private string _imageFolder;
        private string ImageFolder
        {
            get
            {
                if (_imageFolder == null)
                    _imageFolder = ConfigurationManager.AppSettings["ImageFolder"];
                if (string.IsNullOrEmpty(_imageFolder))
                    throw new ApplicationException("ImageFolder not configured!");
                return _imageFolder;
            }

        }

        [Authorize]
        public ActionResult Poster(string id)
        {
            if (string.IsNullOrEmpty(id))
                return DefaultPoster();

            var nameWithImageUrl = GetNameAndImageUrlForMovie(id);
            if (nameWithImageUrl.IsDefault)
                return DefaultPoster();

            var fn = sysIO.Path.Combine(ImageFolder, nameWithImageUrl.Name);

            if (!sysIO.File.Exists(fn) && !string.IsNullOrEmpty(nameWithImageUrl.ImageUrl))
                (new WebClient()).DownloadFile(nameWithImageUrl.ImageUrl, fn);

            Response.Expires = 60 * 24 * 7;
            if (sysIO.File.Exists(fn))
                return base.File(fn, "image/jpeg");
            else
                return DefaultPoster();
        }

        private ActionResult DefaultPoster()
        {
            Response.Cache.SetExpires(DateTime.Now + TimeSpan.FromSeconds(2));
            return base.File("~/Content/images/noPoster.png", "image/png");
        }

        private static string SafeMovieFileName(Movie movie)
        {
            string safeTitle = Regex.Replace(movie.Title, @"[^a-z0-9_-]", "_", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Trim();
            return string.Format("{0}_{1}.jpg", safeTitle, movie.ImdbId);
        }
        #endregion
    }
}
