using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epidaurus.ScannerLib.SourceScanning
{
    public class ScannerFactory : IScannerFactory
    {
        public IScanner CreateFromTypeId(string typeId, string data1, string data2, string rebase)
        {
            switch (typeId)
            {
                case "Folder":
                    return new FolderScanner(data1, rebase);
                default:
                    throw new ArgumentOutOfRangeException("Unknown type", "typeId");
            }
        }
    }
}
