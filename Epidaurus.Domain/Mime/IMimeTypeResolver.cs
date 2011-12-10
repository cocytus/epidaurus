using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epidaurus.Domain.Mime
{
    public interface IMimeTypeResolver
    {
        string Resolve(string fileName);
        string Resolve(string fileName, string defaultMimeType);
    }
}
