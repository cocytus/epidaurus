using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epidaurus.ScannerLib.SourceScanning
{
    public interface IScanner
    {
        DirResult Scan();
    }
}
