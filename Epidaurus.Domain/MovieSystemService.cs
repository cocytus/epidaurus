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

		public void AddMovie(Movie movie)
		{
			if (movie.MovieSystemService == null)
				movie.MovieSystemService = this;
			DbEntities.AddToMovies(movie);
			Save();
		}

		public IEnumerable<Movie> GetMoviesThatShouldBeUpdated()
		{
			return DbEntities.Movies.Where(m => !m.ImdbQueried && m.ImdbQueryFailCount < 5 && m.MovieAtStorages.Any(mas => !mas.Ignore)).ToList();
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
				if (existingMovie.Id == movie.Id)
					throw new InvalidOperationException("Insanity achieved. SetImdbIdOnMovie...");

				foreach (var source in movie.MovieAtStorages.ToList())
					existingMovie.MovieAtStorages.Add(source);

				DbEntities.DeleteObject(movie);
				Save();

				return existingMovie;
			}
			else
			{
				movie.ImdbId = newImdbId;
				Save();
				return movie;
			}
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

		public Person GetOrCreatePerson(string name)
		{
			var person = DbEntities.People.FirstOrDefault(el => el.Name == name);
			if (person == null)
			{
				person = new Person() { Name = name };
				DbEntities.AddToPeople(person);
				Save();
			}
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
