using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Epidaurus.Domain.Entities;

namespace Epidaurus.ViewModels
{
    public enum SeenNotSeen
    {
        NotSeen,
        Seen,
    }

    public class MovieListViewModel
    {
        public IEnumerable<Movie> Movies;
        public MovieFilterViewModel MovieFilter;
        public UserViewModel CurrentUser;
        public Dictionary<int, List<string>> WhoHasSeenMovie;
        public List<System.Web.Mvc.SelectListItem> SortByList { get; set; }
        public List<System.Web.Mvc.SelectListItem> GenreList { get; set; }
        public List<System.Web.Mvc.SelectListItem> PersonList { get; set; }

        public List<UserViewModel> Users { get; set; }
        public int TotalPlayTime { get; set; }
    }

    public class UserViewModel
    {
        public static explicit operator UserViewModel(User user)
        {
            if (user == null)
                return null;

            return new UserViewModel()
            {
                Username = user.Username,
                Name = user.Name,
                LastLogin = user.LastLogin,
                IsAdmin = user.IsAdmin
            };
        }

        public bool IsAdmin { get; set; }
        public DateTime LastLogin { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
    }

    public class MovieFilterViewModel
    {
        public MovieFilterViewModel()
        {
            SelectedUsers = new string[0];
        }

        public string Search { get; set; }
        public string SortBy { get; set; }
        public int? Person { get; set; }
        public int? Genre { get; set; }
        public string[] SelectedUsers { get; set; }
        public SeenNotSeen SeenNotSeen { get; set; }
    }

    public class MovieViewModel
    {
        private readonly Movie _movie;
        private Lazy<string> _youtubeTrailerUrl;

        public MovieViewModel(Movie movie)
        {
            _movie = movie;
            _youtubeTrailerUrl = new Lazy<string>(TryGetYoutubeId);
        }

        public Movie Movie { get { return _movie; } }

        public IList<Cast> Directors
        {
            get
            {
                return GetSortedJob(Cast.Jobs.Director);
            }
        }

        public IList<Cast> Writers
        {
            get
            {
                return GetSortedJob(Cast.Jobs.Writer);
            }
        }

        public IList<Cast> Actors
        {
            get
            {
                return GetSortedJob(Cast.Jobs.Actor);
            }
        }

        public bool HasSample
        {
            get
            {
                return _movie.MovieAtStorages.Any(el => el.SampleRelativePath != null);
            }
        }

        private string TryGetYoutubeId()
        {
            var turl = Movie.TrailerUrl;
            if (string.IsNullOrEmpty(turl))
                return null;
            var turlL = turl.ToLowerInvariant();
            if (!turlL.Contains("youtube") && !turl.Contains("youtu.be"))
                return null;

            var m = Regex.Match(turl, @"[/=]([A-Za-z0-9_-]{10,12})(?:[/&]|$)", RegexOptions.CultureInvariant | RegexOptions.Multiline);
            return m.Success ? m.Groups[1].Value : null;
        }

        public string YoutubeTrailerId
        {
            get { return _youtubeTrailerUrl.Value; }
        }

        private IList<Cast> GetSortedJob(Cast.Jobs job)
        {
            if (!_movie.Casts.IsLoaded)
                _movie.Casts.Load();
            var jobStr = job.ToString();
            return (from c in _movie.Casts where c.Job == jobStr orderby c.SortOrder select c).ToList();
        }
    }
}