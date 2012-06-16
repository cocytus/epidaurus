using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Epidaurus.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using Epidaurus.Domain.DataValidation;
using Epidaurus.Security;

namespace Epidaurus.ViewModels.UserEditorViewModels
{
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
                Password = user.Password,
                Roles = user.Roles,
            };
        }

        [Required]
        [StringLength(64, MinimumLength=2)]
        public string Username { get; set; }

        public string Password { get; set; }

        [Required]
        [StringLength(64, MinimumLength = 2)]
        public string Name { get; set; }

        [SpearatedListOf(',', new string[] { EpiRoles.Admin, EpiRoles.Downloader, EpiRoles.NoFullScreenImages }, AllowEmpty=true, CleanWhitespace=true)]
        public string Roles { get; set; }

        public DateTime LastLogin { get; set; }
    }
}
