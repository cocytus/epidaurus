using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Epidaurus.ScannerLib.SourceScanning
{
    [DebuggerDisplay("MSR: '{CleanedName}' Path: '{Path}'")]
    public class MovieScanResult
    {
        public string Path;
        public string CleanedName;
        public short? Year;
        public string ImdbId;
        public string SamplePath;
        public short? SeriesSeason;
        public short? SeriesEpisode;
    }
}
