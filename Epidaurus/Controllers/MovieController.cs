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

namespace Epidaurus.Controllers
{
    public class MovieController : Controller
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private readonly MovieSystemService _movieSystemService;
        private readonly MovieInformationUpdater _movieInformationUpdater;

        public MovieController(MovieSystemService movieSystemService, MovieInformationUpdater movieInformationUpdater)
        {
            _movieSystemService = movieSystemService;
            _movieInformationUpdater = movieInformationUpdater;
        }

        // GET: /Movie/
        [Authorize]
        public ActionResult Index(MovieFilterViewModel mi)
        {
            var db = _movieSystemService.DbEntities;
            var vm = CreateMovieListViewModel(mi);
            var movies = MoviesWithReferences;

            movies = movies.Where(m => m.MovieAtStorages.Any(mas => !mas.Ignore));

            if (!string.IsNullOrEmpty(mi.Search))
                movies = movies.Where(e => e.Title.Contains(mi.Search));

            if (mi.Person.HasValue)
                movies = movies.Where(e => e.Actors.Any(d => d.Id == mi.Person.Value) || e.Writers.Any(d => d.Id == mi.Person.Value) || e.Directors.Any(d => d.Id == mi.Person.Value));
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

            List<Movie> movieList;
            if (mi.SelectedUsers != null && mi.SelectedUsers.Length > 0)
            {
                if (mi.SeenNotSeen == SeenNotSeen.NotSeen)
                {
                    var idFilter = new HashSet<int>(from user in db.Users from ss in user.SeenStatuses where mi.SelectedUsers.Contains(user.Username) select ss.Movie.Id);
                    movieList = movies.ToList().Where(m => !idFilter.Contains(m.Id)).ToList();
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
                    movieList = movies.ToList().Where(m => idFilter.Contains(m.Id)).ToList();
                }
            }
            else
                movieList = movies.ToList();
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
                    .Include("MovieAtStorages")
                    .Include("MovieAtStorages.StorageLocation")
                    .Include("Directors")
                    .Include("Genres")
                    .Include("Writers")
                    .Include("Actors")
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
                    var dcount = from el in _movieSystemService.DbEntities.People orderby el.Name select new { Person = el, Count = el.MoviesWhereActor.Count() + el.MoviesWhereDirector.Count() + el.MoviesWhereWriter.Count() };
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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult UpdateMovie(int id)
        {
            try
            {
                var movie = _movieSystemService.GetMovieById(id);
                _movieInformationUpdater.UpdateMovieFromDataSource(movie);
                _movieSystemService.Save();
                return View("MovieListEntry", movie);
            }
            catch (Exception ex)
            {
                _log.ErrorException(string.Format("UpdateMovie failed: id: {0} ", id), ex);
                return this.Content(string.Format("<div>ERROR UpdateMovie failed: id: {0} <br/>Exception:<br/>{1}</div>", id, ex.ToString()));
            }
        }

        [HttpPost]
        [Authorize(Roles="Admin")]
        public ActionResult SetMovieImdbId(int id, string imdbId)
        {
            try
            {
                imdbId = MovieInformationUpdater.CleanImdbId(imdbId);
                var movie = _movieSystemService.SetImdbIdOnMovie(id, imdbId);
                _movieInformationUpdater.UpdateMovieFromDataSource(movie);
                _movieSystemService.Save();
                return View("MovieListEntry", movie);
            }
            catch (Exception ex)
            {
                _log.ErrorException(string.Format("SetMovieImdbId failed: id: {0} imdbId: {1}", id, imdbId), ex);
                return this.Content(string.Format("<div>ERROR SetMovieImdbId failed: id: {0} imdbId: {1} <br/>Exception:<br/>{2}</div>", id, imdbId, ex.ToString()));
            }
        }
         
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public JsonResult IgnoreSource(int id)
        {
            _movieSystemService.IgnoreMovieSource(id);
            return new JsonResult() { Data = "OK" };
        }

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

        private Dictionary<string, NameWithImageUrl> SafeFileNameCache
        {
            get 
            {
                lock (_safeFileNameCacheLock)
                {
                    if (_safeFileNameCache == null)
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


        [Authorize]
        public ActionResult Poster(string id)
        {
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
