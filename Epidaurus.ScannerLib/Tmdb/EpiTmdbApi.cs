using System;
using System.Collections.Generic;
using System.Configuration;
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

            }
        }

        protected string ApiKey
        {
            get { return ConfigurationManager.AppSettings["tmdbApiKey"].Trim(); }
        }
    }
}
