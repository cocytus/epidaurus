using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Epidaurus.Domain;
using Epidaurus.ScannerLib;
using Epidaurus.ScannerLib.SourceScanning;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Scanner
{
    [TestClass]
    public class TestStorageLocationScanning
    {
        [TestMethod]
        [Ignore] //Has side effects.
        public void TestAllStorageLocationsScan()
        {
            var bah = new MovieSourceScanner(new MovieSystemService(), new ScannerFactory());
            bah.ScanAllStorageLocations();
        }
    }
}
