using System;
using System.Collections.Generic;
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
            return (from el in GoogleApi.Search(query)
                    let m = _rexImdbTitle.Match(el.Url)
                    where m.Success
                    select m.Groups[1].Value).ToList();
        }
    }
}
