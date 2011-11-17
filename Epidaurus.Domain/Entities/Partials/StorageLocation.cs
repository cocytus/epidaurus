using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epidaurus.Domain.Entities
{
    public partial class StorageLocation : IEntityWithMovieSystemServiceKnowledge
    {
        public MovieSystemService MovieSystemService { get; set; }
    }
}
