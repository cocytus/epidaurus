using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;


namespace Epidaurus.Domain.Entities
{
    [MetadataType(typeof(UserMetaData))]
    public partial class User
    {
        public class UserMetaData
        {
            [Required]
            [StringLength(64, MinimumLength=2)]
            public string Username { get; set; }

            [Required]
            [StringLength(64, MinimumLength=8)]
            public string Password { get; set; }

            [Required]
            [StringLength(64, MinimumLength = 2)]
            public string Name { get; set; }
        }
    }
}
