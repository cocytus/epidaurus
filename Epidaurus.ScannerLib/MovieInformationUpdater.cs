﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using System.Net;
using Epidaurus.Domain;
using Epidaurus.Domain.Entities;
using Epidaurus.ScannerLib.Tmdb;

namespace Epidaurus.ScannerLib
{
    public class MovieInformationUpdater
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private readonly MovieSystemService _movieSystemService;

        public MovieInformationUpdater(MovieSystemService movieSystemService)
        {
            _movieSystemService = movieSystemService;
        }

        public void UpdateAllMoviesInDatabase()
        {
            foreach (var movie in _movieSystemService.GetMoviesThatShouldBeUpdated().Take(50))
            {
                if (string.IsNullOrEmpty(movie.ImdbId))
                    TryFindMovieIdAndUpdateMovie(movie);
                else
                    if (!UpdateMovieFromDataSource(movie))
                    {
                        _log.Warn("UpdateMovieFromImdb returned false, not updating any more movies.");
                        break;
                    }
                _movieSystemService.Save();
            }

            _movieSystemService.Save();
        }

        /// <summary>
        /// Requests info from imdb if movie does not already exist.
        /// </summary>
        /// <param name="imdbId"></param>
        /// <returns></returns>
        public Movie GetOrCreateNewMovieWithImdbId(string imdbId)
        {
            imdbId = CleanImdbId(imdbId);
            if (string.IsNullOrEmpty(imdbId))
                throw new ArgumentNullException("imdbId", "ImdbId kan ikke være null");

            var movie = _movieSystemService.TryGetMovieByImdbId(imdbId);
            if (movie == null)
            {
                movie = Movie.Create("IMDB ID " + imdbId, imdbId);
                _movieSystemService.AddMovie(movie);
                UpdateMovieFromDataSource(movie);
                _movieSystemService.Save();
            }

            return movie;
        }

        public bool UpdateMovieFromDataSource(Movie movie)
        {
            if (string.IsNullOrEmpty(movie.ImdbId) && (movie.TmdbId == null || movie.TmdbId.Value == 0))
                throw new ArgumentNullException("No IMDB id!");

            Task<int> getScores = null;

            try
            {
                try
                {
                    //Overview: if we have an IMDB, start fetching the ImdbApi score asynchronously, then fetch Tmdb data.
                    //After Tmdb data fetch, if either we did not have an imdb id beforehand, of if the imdbid changed, update the score from imdb synchronously.

                    if (!string.IsNullOrEmpty(movie.ImdbId))
                        getScores = Task.Factory.StartNew<int>(() => Imdb.ImdbApi.QuickScoreFetcher(movie.ImdbId));
                    var requestedImdbId = movie.ImdbId;

                    var ret = UpdateMovieFromTmdb(movie);

                    if (getScores != null)
                    {
                        try
                        {
                            movie.Score = getScores.Result;
                            getScores.Dispose();
                            getScores = null;
                        }
                        catch (AggregateException ex)
                        {
                            _log.Error("UpdateMovieFromDataSource GetMovieScoresFromImdbAsync: {0}", ex.InnerException.Message);
                            movie.Score = -2;
                        }
                    }

                    if ((getScores == null || movie.ImdbId != requestedImdbId) && !string.IsNullOrEmpty(movie.ImdbId))
                    {
                        try
                        {
                            _log.Debug("Fetching imdb score synchronously. MovieId {0} Old ImdbId {1} New ImdbId {2}", movie.Id, requestedImdbId, movie.ImdbId);
                            movie.Score = Imdb.ImdbApi.QuickScoreFetcher(movie.ImdbId);
                        }
                        catch (Exception ex)
                        {
                            _log.Error("UpdateMovieFromDataSource Sync QuickScoreFetcher failed with: {0}", ex.InnerException.Message);
                            movie.Score = -3;
                        }
                    }

                    return ret;
                }
                catch (TmdbNotConfiguredException)
                {
                    return UpdateMovieFromImdb(movie);
                }
            }
            catch (Exception ex)
            {
                _log.Error("{0} update failure: {1}", movie.ImdbId, ex.ToString()); //This should not update fail count...
                return !(ex is WebException); //If webexception, return false to indicate break of loop
            }
            finally
            {
                if (getScores != null) //We need to observe any exceptions, thus this code.
                {
                    getScores.Wait(5000);
                    try
                    {
                        var hm = getScores.Result;
                    }
                    catch
                    {
                        _log.Error("In finally, getscores.Result failed...");
                    }
                }
            }
        }

        private void TryFindMovieIdAndUpdateMovie(Movie movie)
        {
            _log.Trace("Trying to find imdb id for " + movie.Title);
            var newImdbId = Imdb.ImdbApi.ImdbIdFinder(movie.Title, movie.Year > 1800 ? (int?)movie.Year : null);
            if (!string.IsNullOrEmpty(newImdbId))
            {
                var newMovie = _movieSystemService.SetImdbIdOnMovie(movie.Id, newImdbId);
                if (!newMovie.ImdbQueried)
                    UpdateMovieFromDataSource(newMovie);
            }
        }

        private bool UpdateMovieFromTmdb(Movie movie)
        {
            var tmdb = new EpiTmdbApi();
            var result = (movie.TmdbId.HasValue && movie.TmdbId.Value > 0) ? 
                tmdb.QueryMovieByTmdbId(movie.TmdbId.Value) :
                tmdb.QueryMovieByImdbId(movie.ImdbId);

            if (result != null)
            {
                movie.ImdbQueried = true;
                movie.Plot = result.Plot;
                movie.Title = result.Title;
                movie.Year = result.Year;
                movie.Score = result.Score;
                movie.ImageUrl = result.Poster;
                movie.Runtime = result.Runtime;
                movie.Homepage = result.Homepage;
                movie.SetGenres(result.Genres);
                movie.TrailerUrl = result.TrailerUrl;
                if (!string.IsNullOrEmpty(result.ImdbId))
                    movie.ImdbId = result.ImdbId;

                movie.Casts.ToList().ForEach(_movieSystemService.DbEntities.DeleteObject);
                foreach (var cast in result.Casts)
                    movie.AddCastMember(cast.Job, cast.Name, cast.ImdbId, cast.TmdbId, cast.SortOrder, cast.RoleName);

                movie.ImdbQueryFailCount = 0;
                movie.TmdbId = result.TmdbId;
                _log.Info("TmdbApi successful query {0}({1}) : {2}", movie.TmdbId, movie.ImdbId, movie.Title);
            }
            else
            {
                _log.Error("TmdbApi FAILED query {0} : {1}", movie.ImdbId, movie.Title);
                movie.ImdbQueryFailCount = movie.ImdbQueryFailCount <= 5 ? 10 : movie.ImdbQueryFailCount + 1;
            }

            return true;
        }

        private bool UpdateMovieFromImdb(Movie movie)
        {
            var imdbResult = Imdb.ImdbApi.GetInfo(movie.ImdbId);
            if (imdbResult != null)
            {
                movie.ImdbQueried = true;
                movie.Plot = imdbResult.Plot;
                movie.Title = imdbResult.Title;
                movie.Year = short.Parse(imdbResult.Year);
                if (imdbResult.Rating != "N/A")
                    movie.Score = (int) (double.Parse(imdbResult.Rating, CultureInfo.InvariantCulture.NumberFormat)*10.0);
                else
                    movie.Score = -1;
                movie.ImageUrl = imdbResult.Poster;
                movie.Runtime = RuntimeParser(imdbResult.Runtime);

                UpdateGenresInfo(movie, imdbResult.Genre);
                UpdatePeopleInfo(movie, imdbResult);
                movie.ImdbQueryFailCount = 0;
                _log.Info("ImdbApi successful query {0} : {1}", movie.ImdbId, movie.Title);
            }
            else
            {
                _log.Error("ImdbApi FAILED query {0} : {1}", movie.ImdbId, movie.Title);
                movie.ImdbQueryFailCount += 1;
            }

            return true;
        }

        public static string CleanImdbId(string newImdbId)
        {
            int id;
            if (int.TryParse(newImdbId, out id))
                return "tt" + id.ToString();
            var m = Regex.Match(newImdbId, @".*(tt[0-9]{7})", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (m.Success)
                return m.Groups[1].Value;
            else
                throw new ArgumentException("newImdbId", "Invalid IMDB id");
        }

        private static int? RuntimeParser(string runtime)
        {
            var m = Regex.Match(runtime, @"^(?:([0-9]+)\s*h[a-z\s]+)?(?:([0-9]+)\s*m[a-z\s]+)?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (!m.Success)
                return null;
            int minutes = 0;
            if (m.Groups[1].Success && !string.IsNullOrEmpty(m.Groups[1].Value))
                minutes = int.Parse(m.Groups[1].Value) * 60;
            if (m.Groups[2].Success && !string.IsNullOrEmpty(m.Groups[2].Value))
                minutes += int.Parse(m.Groups[2].Value);

            return minutes == 0 ? (int?)null : minutes;
        }

        private static void UpdateGenresInfo(Movie movie, string genresStr)
        {
            movie.SetGenres(from g in genresStr.Split(',') let trimmed = g.Trim() where trimmed.Length > 0 select trimmed);
        }

        private static void UpdatePeopleInfo(Movie movie, ImdbSearchResult imdbResult)
        {
            throw new NotImplementedException(); //TODO: Will be removed when ImdbApi uses new MovieDataSource thingy
            //movie.SetDirectors(from d in imdbResult.Director.Split(',') let dt = d.Trim() where dt.Length > 0 select dt);
            //movie.SetActors(from d in imdbResult.Actors.Split(',') let dt = d.Trim() where dt.Length > 0 select dt);
            //movie.SetWriters(from d in imdbResult.Writer.Split(',') let dt = d.Trim() where dt.Length > 0 select dt);
        }
    }
}
