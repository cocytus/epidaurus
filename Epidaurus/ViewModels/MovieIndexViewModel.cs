using System;
using System.Collections.Generic;
using System.Linq;
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
}