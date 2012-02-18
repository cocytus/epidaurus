using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Net;
using System.Web;
using System.Configuration;
using System.Text.RegularExpressions;
using Epidaurus.ScannerLib.Utils;

namespace Epidaurus.ScannerLib.Imdb
{
    public static class ImdbApi
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        //TODO: This should return MovieDataSourceQueryResult instead.
        public static ImdbSearchResult GetInfo(string imdbId)
        {
            string uri = string.Format("http://www.imdbapi.com/?i={0}", imdbId);

            var ret = Json.JsonDeserialize<ImdbSearchResult>(uri);
            if (ret.Response != "True")
                return null;
            if (ret.Poster.Length < 20)
                ret.Poster = null;
            return ret;
        }

        private static readonly Regex _rexImdbTitle = new Regex(@"http://.*imdb\.com/title/(tt[0-9]+)/", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        public static string ImdbIdFinder(string movieName, int? year = null)
        {
            var hm = SearchForPossibleImdbIds(movieName, year);
            return hm.FirstOrDefault();
        }

        public static bool IsValidImdbTitleUrl(string url)
        {
            return _rexImdbTitle.IsMatch(url);
        }

        private static IList<string> SearchForPossibleImdbIds(string movieName, int? year)
        {
            var query = movieName + (year.HasValue ? " " + year.Value.ToString() : "") + " site:imdb.com";
            var googleResult = GoogleApi.Search(query);
            if (googleResult == null)
            {
                _log.Warn("Google search for '{0}' (year: '{1}') returned nulll", movieName, year);
                return new List<string>();
            }

            return (from el in googleResult
                    let m = _rexImdbTitle.Match(el.Url)
                    where m.Success
                    select m.Groups[1].Value).ToList();
        }

        public static int QuickScoreFetcher(string imdbId)
        {
            var url = "http://m.imdb.com/title/" + imdbId;
            var wr = (HttpWebRequest)WebRequest.Create(url);
            wr.UserAgent = "Mozilla";
            using (var response = wr.GetResponse())
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                var data = sr.ReadToEnd();
                var m = Regex.Match(data, @"votes\"">\s+<strong>([0-9\.]+)");
                if (!m.Success)
                    throw new InvalidOperationException(string.Format("No score found for imdb {0}", imdbId));
                return (int)(double.Parse(m.Groups[m.Groups.Count - 1].Value, CultureInfo.InvariantCulture) * 10.0);
            }
        }
    }
}
