using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using TheMovieDb;
using Epidaurus.Domain.Entities;

namespace Epidaurus.ScannerLib.Tmdb
{
    public class EpiTmdbApi
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private readonly TmdbApi _tmdbApi;

        public EpiTmdbApi()
        {
            var apiKey = ApiKey;
            if (string.IsNullOrEmpty(apiKey) || apiKey.ToLower().Contains("your"))
                throw new TmdbNotConfiguredException();

            _tmdbApi = new TmdbApi(apiKey);
        }

        protected string ApiKey
        {
            get { return (ConfigurationManager.AppSettings["tmdbApiKey"] ?? "").Trim(); }
        }

        public MovieDataSourceQueryResult QueryMovieByImdbId(string imdbId)
        {
            IList<TmdbMovie> movies;
            try 
            { 
                movies = _tmdbApi.MovieSearchByImdb(imdbId).ToList(); 
            }
            catch (SerializationException)
            {
                _log.Error("QMBII: Serialization error for imdb id {0}", imdbId);
                return null;
            }

            if (movies.Count == 0)
            {
                _log.Warn("QMBII: No result for IMDB ID {0}", imdbId);
                return null;
            }
            else if (movies.Count > 1)
            {
                _log.Error("QMBII: Got more than one movie for imdbId {0} : {1}", imdbId, string.Join(", ", from mo in movies select mo.Name));
                return null;
            }

            //The movie does not contain cast, we need to query again with imdb id to get this.
            return QueryMovieByTmdbId(movies[0].Id);
        }

        public MovieDataSourceQueryResult QueryMovieByTmdbId(int tmbdId)
        {
            var m = _tmdbApi.GetMovieInfo(tmbdId);
            if (m.Cast == null)
                m.Cast = new List<TmdbCastPerson>();
            if (m.Genres == null)
                m.Genres = new List<TmdbGenre>();

            return new MovieDataSourceQueryResult
                       {
                           Title = (string.IsNullOrEmpty(m.OriginalName) || m.OriginalName == m.Name) ? m.Name : string.Format("{0} ({1})", m.OriginalName, m.Name),
                           ImdbId = m.ImdbId,
                           TmdbId = m.Id,
                           Plot = m.Overview,
                           Runtime =  !string.IsNullOrWhiteSpace(m.Runtime) ? int.Parse(m.Runtime) : 0,
                           Score = (int) (double.Parse(m.Rating, CultureInfo.InvariantCulture)*10.0),
                           Votes = int.Parse(m.Votes),
                           Poster = GetPoster(m.Posters),
                           Homepage = !string.IsNullOrWhiteSpace(m.Homepage) ? m.Homepage : null,
                           Year = !string.IsNullOrEmpty(m.Released) ? (short) DateTime.Parse(m.Released).Year : (short)-1,
                           Casts = (from c in m.Cast let cc = CreateCast(c) where cc.HasValue select cc.Value).Distinct().ToArray(),
                           Genres = (from g in m.Genres select g.Name).ToArray(),
                           TrailerUrl = !string.IsNullOrWhiteSpace(m.Trailer) ? m.Trailer : null
                       };
        }

        private static MovieDataSourcePersonData? CreateCast(TmdbCastPerson c)
        {
            var jobMap = new [] 
            { 
                new { j = Cast.Jobs.Actor, jobs = new string[] { "actor" } },
                new { j = Cast.Jobs.Director , jobs = new string[] { "director" } },
                new { j = Cast.Jobs.Writer, jobs = new string[] { "author", "novel", "screenplay", "writer" } } 
            };

            var job = (from j in jobMap where j.jobs.Contains(c.Job.ToLowerInvariant()) select j.j).FirstOrDefault();
            if (job == default(Cast.Jobs))
                return null;

            return new MovieDataSourcePersonData(c.Name, null, c.Id, c.Order, job, c.Character);
        }
      
        private string GetPoster(IList<TmdbImage> posters)
        {
            var img =
                posters.FirstOrDefault(el => el.ImageInfo.Type == "poster" && el.ImageInfo.Size == "mid") ??
                posters.FirstOrDefault(el => el.ImageInfo.Type == "poster" && el.ImageInfo.Size == "original") ??
                posters.FirstOrDefault();
            return img != null ? img.ImageInfo.Url : null;
        }
    }

    public class TmdbNotConfiguredException : Exception
    {
        public TmdbNotConfiguredException() : base("TMDB api key not configured")
        {
        }
    }
}
