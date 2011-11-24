using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Epidaurus.ScannerLib.Tmdb
{
    class EpiTmdbApi
    {
        public EpiTmdbApi()
        {
            var api = new TheMovieDb.TmdbApi(ApiKey);

            var titles = api.MovieSearch("the matrix");

            foreach (var title in titles)
            {
                Trace.WriteLine(string.Format("Eh: ID: {3} {0} hm: {1} hm2: {2}", title.Name, title.OriginalName, title.ImdbId, title.Id));
            }

        }

        protected string ApiKey
        {
            get { return ConfigurationManager.AppSettings["tmdbApiKey"].Trim(); }
        }
    }
}
