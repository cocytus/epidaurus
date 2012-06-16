using System;
using System.Collections.Generic;
using System.Linq;
using Epidaurus.ScannerLib.SourceScanning;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Diagnostics;

namespace Test.Scanner
{
    [TestClass]
    public class ScannerTests
    {
        [TestMethod]
        public void TestFolderScan()
        {
            var fs = ScanTestFolder();
            Trace.WriteLine("Hmm: fs" + fs.ToString());
        }

        private DirResult ScanTestFolder()
        {
            var fullPath = GetTestDirPath();
            var scanner = (new ScannerFactory()).CreateFromTypeId("Folder", fullPath, null, "");
            return scanner.Scan();
        }

        private static string GetTestDirPath()
        {
            var baseDir = "Test.Scanner\\TestDir";

            for (int i = 0; i < 10; i++)
            {
                var test = string.Join("", Enumerable.Range(0,i).Select(cnt=>"..\\")) + baseDir;
                var path = Path.GetFullPath(test);
                if (Directory.Exists(path))
                    return path;
            }
            throw new InvalidOperationException("Could not find scanner testdir");
        }

        [TestMethod]
        public void TestFolderScanParsed()
        {
            MovieFinder.MinimumMovieSize = 1;
            var rootDirResult = ScanTestFolder();
            var rawMovieResults = MovieFinder.FindMovies(rootDirResult).ToArray();
            Trace.WriteLine(rawMovieResults);

            var thj = rawMovieResults.Single(sr => sr.Path == @"\Voksenfilmer\Fancy.Movie.2003.720p.BluRay.DTS.5.1.x264-BLAHBLAH\Fancy.Movie.2003.720p.BluRay.DTS.5.1.x264-BLAHBLAH.mkv");
            Assert.AreEqual(@"\Voksenfilmer\Fancy.Movie.2003.720p.BluRay.DTS.5.1.x264-BLAHBLAH\Sample\blappy.mkv", thj.SamplePath);
            Assert.AreEqual("Fancy Movie", thj.CleanedName);
            Assert.AreEqual("tt8765432", thj.ImdbId);
            Assert.AreEqual((short)2003, thj.Year);

            Assert.IsFalse(thj.SeriesSeason.HasValue);
            Assert.IsFalse(thj.SeriesEpisode.HasValue);

            var bf1 = rawMovieResults.Single(sr => sr.Path == @"\Barnefilmer\Barnefilm.mkv");
            Assert.IsNull(bf1.SamplePath);
            var bf2 = rawMovieResults.Single(sr => sr.Path == @"\Barnefilmer\Barnefilm2.avi");
            Assert.IsNull(bf1.SamplePath);

            var simp1 = rawMovieResults.Single(sr => sr.Path == @"\Serier\Simpsons\The.Simpsons.S22E08.The Fight Before Christmas.720p.WEB-DL.DD5.1.H.264-LP.mkv");
            Assert.AreEqual(simp1.SeriesSeason.Value, 22);
            Assert.AreEqual(simp1.SeriesEpisode.Value, 8);

            var hm = rawMovieResults.Single(sr => sr.Path == @"\Voksenfilmer\Some movie on DVD 2003\file01.vob");
            Assert.AreEqual(@"\Voksenfilmer\Some movie on DVD 2003\sample.mkv", hm.SamplePath);

            Assert.IsFalse(rawMovieResults.Any(sr => sr.Path == @"\Voksenfilmer\Some movie on DVD 2003\file02.vob"));

            foreach (var mr in rawMovieResults)
            {
                var seriesText = mr.SeriesEpisode.HasValue ? string.Format("Series: S{0}E{1} ", mr.SeriesSeason.HasValue ? mr.SeriesSeason.Value.ToString() : "xx", mr.SeriesEpisode.Value) : "";
                Trace.WriteLine(string.Format("Cleaned: '{0}' {1}Path: '{2}' ImdbId: '{3}' Sample: '{4}'", mr.CleanedName, seriesText, mr.Path, mr.ImdbId, mr.SamplePath));
            }
        }

        private void TestScanningLocation(string location)
        {
            var sw = Stopwatch.StartNew();
            var scanner = (new ScannerFactory()).CreateFromTypeId("Folder", location, null, "");
            MovieFinder.MinimumMovieSize = 100000000;
            var movies = MovieFinder.FindMovies(scanner.Scan());
            Trace.WriteLine(string.Format("Scanning took {0}ms", sw.ElapsedMilliseconds));

            foreach (var mr in movies)
            {
                var seriesText = mr.SeriesEpisode.HasValue ? string.Format("Series: S{0}E{1} ", mr.SeriesSeason.HasValue ? mr.SeriesSeason.Value.ToString() : "xx", mr.SeriesEpisode.Value) : "";
                Trace.WriteLine(string.Format("Cleaned: '{0}' {1}Path: '{2}' ImdbId: '{3}' Sample: {4}", mr.CleanedName, seriesText, mr.Path, mr.ImdbId ?? "No", mr.SamplePath != null ? "Yes" : "No"));
            }
        }
    }
}
