using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Epidaurus.ViewModels
{
    public class LogOnViewModel
    {
        [Required]
        [StringLength(32, MinimumLength = 2)]
        public string Username { get; set; }

        [Required]
        [StringLength(32, MinimumLength=3)]
        public string Password { get; set; }

        [DisplayName("Remember login")]
        public bool RememberLogin { get; set; }
    }
}