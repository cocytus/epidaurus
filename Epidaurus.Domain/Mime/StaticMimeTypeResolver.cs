using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epidaurus.Domain.Mime
{
    public class StaticMimeTypeResolver : IMimeTypeResolver
    {
        public string Resolve(string fileName)
        {
            var ret = Resolve(fileName, null);
            if (ret == null)
                throw new InvalidOperationException("Unknown file type");
            return ret;
        }

        public string Resolve(string fileName, string defaultMimeType)
        {
            var ext = System.IO.Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(ext))
                throw new ArgumentException("filenName", "Not a file name");
            string ret;
            if (!_mimeMap.TryGetValue(ext.ToLowerInvariant(), out ret))
                return defaultMimeType;
            return ret;
        }

        private static Dictionary<string, string> _mimeMap = new Dictionary<string,string>()
        {
            { ".avi", "video/avi" },
            { ".mkv", "video/x-matroska"},
            { ".wmv", "video/x-ms-wmv"},
            { ".mp4", "video/mp4 "}
        };
    }
}
