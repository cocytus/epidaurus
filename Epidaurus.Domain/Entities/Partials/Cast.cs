using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epidaurus.Domain.Entities
{
    public partial class Cast
    {
        public enum Roles
        {
            Unknown,
            Actor,
            Director,
            Writer
        }
    }
}
