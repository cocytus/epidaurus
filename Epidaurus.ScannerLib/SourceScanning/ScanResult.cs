using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Epidaurus.ScannerLib.SourceScanning
{
    [DebuggerDisplay("DirResult: {Name}")]
    public class DirResult
    {
        public DirResult(DirResult parent, string name, string pathElementSeparator, string locallyAccessiblePath=null)
        {
            Parent = parent;
            PathElementSeparator = pathElementSeparator;
            Name = name;
            Files = new List<FileResult>();
            SubDirs = new List<DirResult>();
            _locallyAccessiblePath = locallyAccessiblePath;
        }

        private string _locallyAccessiblePath;

        public string PathElementSeparator { get; private set; }

        public string Name { get; set; }

        public List<FileResult> Files { get; private set; }

        public List<DirResult> SubDirs { get; private set; }
        public DirResult Parent { get; set; }

        public string Path
        {
            get 
            {
                return (Parent == null ? "" : Parent.Path + PathElementSeparator) + Name;
            }
        }

        public string LocallyAccessiblePath
        {
            get
            {
                return _locallyAccessiblePath;
            }
        }
    }

    [DebuggerDisplay("FileResult: {Name}")]
    public class FileResult
    {
        public readonly string Name;
        public readonly long Size;
        public readonly DirResult Parent;

        private readonly string _pathElementSeparator;

        public FileResult(DirResult parent, string name, long size, string pathElementSeparator)
        {
            Parent = parent;
            Name = name;
            Size = size;
            _pathElementSeparator = pathElementSeparator;
        }

        public string Path
        {
            get
            {
                return Parent.Path + _pathElementSeparator + Name;
            }
        }
    }
}
