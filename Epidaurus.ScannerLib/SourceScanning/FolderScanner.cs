using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Configuration;

namespace Epidaurus.ScannerLib.SourceScanning
{
    class FolderScanner : IScanner
    {
        private readonly string _rootPath;
        private readonly string _rebase;

        public FolderScanner(string rootPath, string rebase)
        {
            if (!Path.IsPathRooted(rootPath))
                throw new ArgumentException("Need full path.");

            _rootPath = Path.GetFullPath(rootPath);
            _rebase = rebase;
        }

        public DirResult Scan()
        {
            var di = new DirectoryInfo(_rootPath);
            var dr = Scan(null, di);
            dr.Name = !string.IsNullOrEmpty(_rebase) ? _rebase : _rootPath;
            return dr;
        }

        private static DirResult Scan(DirResult parent, DirectoryInfo di)
        {
            try
            {
                var dirResult = new DirResult(parent, di.Name, @"\", di.FullName);
                dirResult.Files.AddRange(from fi in di.GetFiles() select new FileResult(dirResult, fi.Name, fi.Length, @"\"));
                dirResult.SubDirs.AddRange(from subDir in di.GetDirectories() let scanRes = Scan(dirResult, subDir) where scanRes != null select scanRes);
                return dirResult;
            }
            catch
            {
                return null;
            }
        }
    }
}
