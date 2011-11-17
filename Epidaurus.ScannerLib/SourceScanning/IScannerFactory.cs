using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epidaurus.ScannerLib.SourceScanning
{
    public interface IScannerFactory
    {
        IScanner CreateFromTypeId(string typeId, string data1, string data2, string rebase);
    }
}
