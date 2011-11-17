using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using Epidaurus.Domain;
using Epidaurus.ScannerLib;

namespace Rescanner
{
    class Program
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        static int Main(string[] args)
        {
            var movieSystemService = new MovieSystemService();

            try
            {
                _log.Debug("Rescan started.");
                VerifyAccessToRoots(movieSystemService);

                var sourceScanner = new MovieSourceScanner(movieSystemService, new Epidaurus.ScannerLib.SourceScanning.ScannerFactory());
                sourceScanner.ScanAllStorageLocations();

                _log.Debug("Folder scan done, updating movie data...");

                sourceScanner.ScanAllStorageLocations();

                _log.Debug("Rescan completed");

                return 0;
            }
            catch (Exception ex)
            {
                _log.ErrorException("Error updating!", ex);
                return 1;
            }
        }

        private static void VerifyAccessToRoots(MovieSystemService mss)
        {
            using (var db = mss.DbEntities)
            {
                foreach (var path in from el in db.StorageLocations where el.Active && el.Type == "Folder" select el.Data1)
                {
                    var di = new DirectoryInfo(path);
                    di.GetFiles();
                }
            }
        }
    }
}
