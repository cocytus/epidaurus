using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Transactions;
using Epidaurus.Domain;
using Epidaurus.Domain.Entities;
using Epidaurus.ScannerLib.SourceScanning;

namespace Epidaurus.ScannerLib
{
    public class MovieSourceScanner
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private readonly MovieSystemService _movieSystemService;
        private readonly IScannerFactory _scannerFactory;

        public MovieSourceScanner(MovieSystemService movieSystemService, IScannerFactory scannerFactory)
        {
            _movieSystemService = movieSystemService;
            _scannerFactory = scannerFactory;
        }

        public void ScanAllStorageLocations()
        {
            var storageLocations = _movieSystemService.GetActiveStorageLocations();

            var tasks = (from sl in storageLocations select StartScanStorageLocation(sl)).ToList();
            while (tasks.Count > 0)
            {
                int idx = Task.WaitAny(tasks.ToArray());
                if (!tasks[idx].IsFaulted)
                    ProcessStorageLocationScanResult(tasks[idx].Result.Item1, tasks[idx].Result.Item2);
                else
                    _log.Error("ScanAllStorageLocations: A task failed!");

                tasks.RemoveAt(idx);
            }
        }

        private Task<Tuple<StorageLocation,MovieScanResult[]>> StartScanStorageLocation(StorageLocation sl)
        {
            return Task.Factory.StartNew(() => 
                {
                    try
                    {
                        return Tuple.Create(sl, ScanStorageLocation(sl));
                    }
                    catch(Exception ex)
                    {
                        _log.Error("Failed to scan StorageLocation {0}: {1}", sl.Name, ex.Message);
                        throw;
                    }
                });
        }

        private MovieScanResult[] ScanStorageLocation(StorageLocation sl)
        {
            var scanner = _scannerFactory.CreateFromTypeId(sl.Type, sl.Data1, sl.Data2, sl.Rebase);
            var scanResult = scanner.Scan();
            var movieScanResults = MovieFinder.FindMovies(scanResult).ToArray();
            return movieScanResults;
        }

        private void ProcessStorageLocationScanResult(StorageLocation sl, MovieScanResult[] newMovieList)
        {
            var previousMoviesAtStorages = sl.MovieSources.ToList();

            foreach (var newMovie in newMovieList.Where(nm => !previousMoviesAtStorages.Any(el => el.RelativePath == nm.Path)))
            {
                Movie movie = null;
                if (newMovie.ImdbId != null)
                    movie = _movieSystemService.TryGetMovieByImdbId(newMovie.ImdbId);

                if (movie == null)
                {
                    movie = CreateMovie(newMovie);
                    _movieSystemService.AddMovie(movie);
                    _log.Info("Adding movie: (Id {0}) {1} ({2})", movie.Id, movie.Title, movie.ImdbId);
                }

                movie.AddStorageLocation(sl, newMovie.Path, newMovie.SamplePath, newMovie.CleanedName);
                _log.Info("Movie at storage added: {0} @ {1} path: {2}", movie.Title, sl.Name, newMovie.Path);
                _movieSystemService.Save();
            }

            var hasPaths = new HashSet<string>(from nm in newMovieList select nm.Path);
            foreach (var oldMovieAtStorage in previousMoviesAtStorages)
            {
                if (!hasPaths.Contains(oldMovieAtStorage.RelativePath))
                {
                    oldMovieAtStorage.Movie.RemoveStorageLocation(sl.Id);
                    _log.Info("Location {0} no longer has {1}", sl.Name, oldMovieAtStorage.CleanedName);
                }
            }

            _movieSystemService.Save();
        }

        private static Movie CreateMovie(MovieScanResult newMovie)
        {
            return Movie.Create(newMovie.CleanedName, newMovie.ImdbId, newMovie.Year, newMovie.SeriesSeason, newMovie.SeriesEpisode);
        }
    }
}
