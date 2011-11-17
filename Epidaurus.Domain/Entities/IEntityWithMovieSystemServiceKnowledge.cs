using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epidaurus.Domain.Entities
{
    interface IEntityWithMovieSystemServiceKnowledge
    {
        MovieSystemService MovieSystemService { get; set; }
    }
}
