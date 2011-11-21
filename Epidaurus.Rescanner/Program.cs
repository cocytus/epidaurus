using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using Epidaurus.Domain;
using Epidaurus.ScannerLib;
using System.Threading;

namespace Rescanner
{
    class Program
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        static int Main(string[] args)
        {
            DebugStart();
            return Run();
        }

        private static void DebugStart()
        {
            #if true
            #if DEBUG
            var start = DateTime.Now;
            while (true)
            {
                Thread.Sleep(100);
                if (DateTime.Now - start > TimeSpan.FromSeconds(5))
                    Environment.Exit(1);
                if (Console.KeyAvailable)
                    break;
            }

            Console.ReadKey();
            Console.WriteLine("push a key");
            Console.ReadKey();
            #endif
            #endif
        }

        private static int Run()
        {
            var movieSystemService = new MovieSystemService();

            try
            {
                _log.Debug("Rescan started.");
                VerifyAccessToRoots(movieSystemService);

                var sourceScanner = new MovieSourceScanner(movieSystemService, new Epidaurus.ScannerLib.SourceScanning.ScannerFactory());
                sourceScanner.ScanAllStorageLocations();

                _log.Debug("Folder scan done, updating movie data...");

                MovieInformationUpdater updater = new MovieInformationUpdater(movieSystemService);
                updater.Update();

                _log.Debug("Rescan completed");

                return 0;
            }
            catch (Exception ex)
            {
                _log.ErrorException("Error updating: ", ex);
                return 1;
            }
        }

        private static void VerifyAccessToRoots(MovieSystemService mss)
        {
            foreach (var path in from el in mss.DbEntities.StorageLocations where el.Active && el.Type == "Folder" select el.Data1)
            {
                var di = new DirectoryInfo(path);
                di.GetFiles();
            }
        }
    }
}
