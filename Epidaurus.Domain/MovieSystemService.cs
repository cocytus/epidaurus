using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Epidaurus.Domain
{
	using Entities;

	public class MovieSystemService : IDisposable
	{
		#region Static members
		private static NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
		#endregion

		public void Save()
		{
			DbEntities.SaveChanges();
		}

		#region Movie objects
		public Movie GetMovieById(int movieId)
		{
			try
			{
				return DbEntities.Movies.First(el => el.Id == movieId);
			}
			catch
			{
				throw new InvalidOperationException(string.Format("Movie with id {0} not found", movieId));
			}
		}

		public Movie TryGetMovieByImdbId(string imdbId)
		{
			return DbEntities.Movies.FirstOrDefault(el => el.ImdbId == imdbId);
		}

        public Movie TryGetMovieByTmdbId(int tmdbId)
        {
            return DbEntities.Movies.FirstOrDefault(el => el.TmdbId == tmdbId);
        }

		public void AddMovie(Movie movie)
		{
			if (movie.MovieSystemService == null)
				movie.MovieSystemService = this;
			DbEntities.AddToMovies(movie);
			Save();
		}

		public IEnumerable<Movie> GetMoviesThatShouldBeUpdated()
		{
			return DbEntities.Movies.Where(m => !m.ImdbQueried && m.ImdbQueryFailCount < 15 && m.MovieAtStorages.Any(mas => !mas.Ignore)).ToList();
		}

		public void IgnoreMovieSource(int movieAtStorageId)
		{
			var movieSource = DbEntities.MovieAtStorages.First(m => m.Id == movieAtStorageId);
			movieSource.Ignore = true;
			Save();
		}

		/// <summary>
		/// This method sets the imdb id of a movie. If another movie entry with the same imdbid exists, that movie is returned, and the one passed in as an argument is deleted.
		/// </summary>
		/// <param name="movie">movie</param>
		/// <param name="newImdbId">new imdb id</param>
		/// <returns></returns>
		public Movie SetImdbIdOnMovie(int movieId, string newImdbId)
		{
			if (string.IsNullOrWhiteSpace(newImdbId))
				throw new ArgumentException("Invalid newImdbId");

			var movie = GetMovieById(movieId);

			if (movie.ImdbId == newImdbId)
			{
				_log.Debug("SetImdbIdOnMovie: Requested id change when no change occured? " + Environment.StackTrace);
				return movie;
			}

			var existingMovie = DbEntities.Movies.FirstOrDefault(m => m.ImdbId == newImdbId);
            if (existingMovie != null)
            {
                TransferMovie(movie, existingMovie);
                movie = existingMovie;
            }
            else
            {
                movie.ImdbId = newImdbId;
                movie.TmdbId = null;
            }

			Save();
			return movie;
		}

        //Same semantics as with SetImdbIdOnMovie
        public Movie SetTmdbIdOnMovie(int movieId, int tmdbId)
        {
            if (tmdbId <= 0)
                throw new ArgumentException("tmdbId", "Invalid value");

            var movie = GetMovieById(movieId);
            if (tmdbId == movie.TmdbId)
            {
                _log.Debug("SetTmdbIdOnMovie: Requested id change when no change occured? " + Environment.StackTrace);
                return movie;
            }

            var existingMovie = DbEntities.Movies.FirstOrDefault(el => el.TmdbId == tmdbId);
            if (existingMovie != null)
            {
                TransferMovie(movie, existingMovie);
                movie = existingMovie;
            }
            else
            {
                movie.TmdbId = tmdbId;
                movie.ImdbId = null;
            }


            Save();
            return movie;
        }

        private void TransferMovie(Movie fromMovie, Movie toMovie)
        {
            if (toMovie.Id == fromMovie.Id)
                throw new InvalidOperationException("Insanity achieved. TransferMovie...");

            foreach (var source in fromMovie.MovieAtStorages.ToList())
                toMovie.MovieAtStorages.Add(source);

            DbEntities.DeleteObject(fromMovie);
        }

		public Genre GetOrCreateGenre(string genre)
		{
			var dbGenre = DbEntities.Genres.FirstOrDefault(el => el.Name == genre);
			if (dbGenre == null)
			{
				dbGenre = new Genre() { Name = genre };
				DbEntities.AddToGenres(dbGenre);
			}

			return dbGenre;
		}

        public Person GetOrCreatePerson(string name, string imdbId, int tmdbId)
        {
            var person = DbEntities.People.FirstOrDefault(el => el.Name == name);
            if (person == null)
            {
                person = new Person() { Name = name, ImdbId = imdbId, TmdbId = tmdbId };
                DbEntities.AddToPeople(person);
                Save();
            }

            if (!string.IsNullOrEmpty(imdbId) && !string.IsNullOrEmpty(person.ImdbId))
                if (imdbId != person.ImdbId)
                    _log.Error("GetOrCreatePerson: IMDB ID Mismatch. Name: {0} Stored ID: {1} Wanted ID: {2}", person.Name, person.ImdbId, imdbId);

            return person;
        }
        #endregion

		#region Storage locations
		public IList<StorageLocation> GetActiveStorageLocations()
		{
			return DbEntities.StorageLocations.Where(el => el.Active).ToList();
		}
		#endregion

		#region ToWatch stuff
		public ToWatch AddToWatch(int movieId, string comment)
		{
			var movie = GetMovieById(movieId);
			var toWatch = new ToWatch()
			{
				User = LoggedInUser,
				Movie = movie,
				Comment = comment ?? ""
			};
			DbEntities.AddToToWatches(toWatch);
			Save();
			return toWatch;
		}

		public void RemoveToWatch(int movieId)
		{
			var toWatch = LoggedInUser.ToWatches.FirstOrDefault(el => el.MovieId == movieId);
			if (toWatch == null)
				throw new InvalidOperationException("User does not currently want to watch that movie?");

			DbEntities.DeleteObject(toWatch);
			Save();
		}

		public IList<User> UsersWithToWatch
		{
			get
			{
				return DbEntities.Users.Include("ToWatches").Include("ToWatches.Movie").Include("ToWatches.Movie.Genres").OrderBy(el => el.Username).ToList();
			}
		}
		#endregion

		#region Misc
		//Throws InvalidOperationException if no logged in user
		public User LoggedInUser
		{
			get
			{
				if (Thread.CurrentPrincipal != null && Thread.CurrentPrincipal.Identity != null && Thread.CurrentPrincipal.Identity.IsAuthenticated && !string.IsNullOrEmpty(Thread.CurrentPrincipal.Identity.Name))
					return DbEntities.Users.First(el => el.Username == Thread.CurrentPrincipal.Identity.Name);
				else
					throw new InvalidOperationException("No logged in user");
			}
		}

		private EpidaurusDbContainer _dbEntities;
		/// <summary>
		/// Direct access to the EF Entities. Do not modify objects you get from this at all.
		/// </summary>
		public EpidaurusDbContainer DbEntities
		{
			get
			{
				if (_dbEntities == null)
					_dbEntities = new EpidaurusDbContainer(this);
				return _dbEntities;
			}
		}

		public void Dispose()
		{
			if (_dbEntities != null)
			{
				_dbEntities.Dispose();
			}
		}
		#endregion

		public void RemoveStorageLocationFromMovie(int movieId, int storageLocationId)
		{
            var mas = DbEntities.MovieAtStorages.First(el => el.Movie.Id == movieId && el.StorageLocation.Id == storageLocationId);
            DbEntities.DeleteObject(mas);
		}
    }
}
