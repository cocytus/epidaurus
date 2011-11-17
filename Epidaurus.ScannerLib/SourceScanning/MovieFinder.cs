using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace Epidaurus.ScannerLib.SourceScanning
{
    static class MovieFinder
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private static readonly Regex _validMovieFilename = new Regex(ConfigurationManager.AppSettings["ValidMovieRegex"], RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex _numbers = new Regex(@"[0-9]", RegexOptions.CultureInvariant);
        private static readonly Regex _isInfoFile = new Regex(@".*\.(?:nfo|txt)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex _imdbTitle = new Regex(@"http://www.imdb.com/title/(tt[0-9]+)/", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        
        public static long MinimumMovieSize = 100000000;

        public static IEnumerable<MovieScanResult> FindMovies(DirResult dir)
        {
            if (!dir.Files.Any(f => f.Name == "ignore"))
            {
                var movies = (from fn in dir.Files where fn.Size > MinimumMovieSize && IsValidMovieFileName(fn.Name, false) orderby fn.Name select fn).ToList();

                if (movies.Count > 0)
                {
                    var singleMovie = (movies.Count == 1) || movies.Select(m => m.Name).All(m => _numbers.Replace(m, "") == _numbers.Replace(movies[0].Name, ""));

                    if (singleMovie)
                    {
                        FileResult sampleFile;
                        if (dir.SubDirs.Count == 1 && dir.SubDirs[0].Name.ToLowerInvariant().Contains("sample"))
                            sampleFile = dir.SubDirs[0].Files.Where(f => IsValidMovieFileName(f.Name, null)).FirstOrDefault();
                        else
                            sampleFile = dir.Files.FirstOrDefault(file => IsValidMovieFileName(file.Name, true));

                        yield return MakeMovieResult(movies[0], true, sampleFile, true);
                    }
                    else
                    {
                        foreach (var movie in movies)
                            yield return MakeMovieResult(movie, false, null, false);
                    }
                }

                foreach (var subDir in dir.SubDirs.Where(sd => !sd.Name.ToLowerInvariant().Contains("sample")))
                    foreach (var result in FindMovies(subDir))
                        yield return result;
            }
        }

        private static MovieScanResult MakeMovieResult(FileResult movie, bool isSingleMovieInParentFolder, FileResult sample, bool tryFindImdbData)
        {
            var msr = new MovieScanResult()
            {
                Path = movie.Path,
                ImdbId = tryFindImdbData ? TryFindImdbId(movie) : null,
                SamplePath = sample != null ? sample.Path : null,
            };
            short? year;

            msr.CleanedName = CleanName(movie, isSingleMovieInParentFolder, out year);
            msr.Year = year;

            SetSeriesData(msr, movie);
            return msr;
        }

        private static Regex _rexSeriesSeasonEpisode1 = new Regex(@"[^a-z0-9]s([0-9]{1,2})e([0-9]{1,2})[^0-9a-z]", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static Regex _rexSeriesSeasonEpisode2 = new Regex(@"[^a-z0-9]([0-9]{1,2})x([0-9]{1,2})[^0-9a-z]", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static Regex _rexSeriesEpisode1 = new Regex(@"[^a-z0-9]e([0-9]+)[^0-9a-z]", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static void SetSeriesData(MovieScanResult msr, FileResult movie)
        {
            var m = _rexSeriesSeasonEpisode1.Match(movie.Name);
            if (!m.Success)
                m = _rexSeriesSeasonEpisode2.Match(movie.Name);

            if (m.Success)
            {
                msr.SeriesSeason = short.Parse(m.Groups[1].Value);
                msr.SeriesEpisode = short.Parse(m.Groups[2].Value);
            }
            else
            {
                m = _rexSeriesEpisode1.Match(movie.Name);
                if (m.Success)
                    msr.SeriesEpisode = short.Parse(m.Groups[1].Value);
            }
        }

        private static string TryFindImdbId(FileResult movie)
        {
            if (movie.Parent.LocallyAccessiblePath == null)
                return null;

            foreach (var fn in movie.Parent.Files.Where(f => _isInfoFile.IsMatch(f.Name)))
            {
                var filePath = Path.Combine(fn.Parent.LocallyAccessiblePath, fn.Name);
                try
                {
                    using (var fileStream = new StreamReader(File.OpenRead(filePath), Encoding.Default))
                    {
                        var match = _imdbTitle.Match(fileStream.ReadToEnd());
                        if (match.Success)
                            return match.Groups[1].Value;
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn("Failed to read nfo file ({0})! {1}", filePath, ex.Message);
                }
            }

            return null;
        }

        private static readonly NameSanitizer _nameSanitizer = new NameSanitizer(ConfigurationManager.AppSettings["RemoveWords"]);

        private static string CleanName(FileResult movie, bool canUseParent, out short? year)
        {
            return _nameSanitizer.GetSanitizedName(movie, canUseParent, out year);
        }

        private static bool IsValidMovieFileName(string name, bool? sampleStatus)
        {
            return _validMovieFilename.IsMatch(name) && (!sampleStatus.HasValue || name.ToLowerInvariant().Contains("sample") == sampleStatus.Value);
        }
    }
}
