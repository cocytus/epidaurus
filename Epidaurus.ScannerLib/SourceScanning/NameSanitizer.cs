using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Epidaurus.ScannerLib.SourceScanning
{
    class NameSanitizer
    {
        private Regex _removeWords;

        public NameSanitizer(string removeWordsRegex)
        {
            _removeWords = new Regex(removeWordsRegex, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        public string GetSanitizedName(FileResult movie, bool canUseParent, out short? year)
        {
            string name = movie.Name;
            int idxExt = name.LastIndexOf('.');
            string tName = SantitizeName(name.Substring(0, idxExt));
            string tPath = (canUseParent && movie.Parent.Parent != null) ? SantitizeName(movie.Parent.Name) : "";
            var probable = tName.Length >= tPath.Length ? tName : tPath;

            //Find year
            year = null;
            var match = Regex.Match(probable, @"^(.*)\(([0-9]{4})\)\s*$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
            if (!match.Success)
                match = Regex.Match(probable, @"^(.*)([0-9]{4})\s*$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
            if (match.Success)
            {
                year = short.Parse(match.Groups[2].Value);
                probable = match.Groups[1].Value.Trim();
            }

            return probable;
        }

        private string SantitizeName(string name)
        {
            name = name.Replace("_", " ");
            Match m = Regex.Match(name, @"^(.+?)(-\s*[a-z0-9]*)?$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (m == null || !m.Success)
                throw new InvalidDataException("Wat?");
            name = m.Groups[1].Value; //Release group name removed
            name = _removeWords.Replace(name, ""); //Remove all known words
            name = Regex.Replace(name, @"[\.\- \[\],]+([^\.\- \[\],]*)", mh => !string.IsNullOrWhiteSpace(mh.Groups[1].Value) ? " " + mh.Groups[1].Value : "");

            return name.Trim();
        }
    }
}
