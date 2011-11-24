using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Epidaurus.Domain;
using Epidaurus.ScannerLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Scanner
{
    [TestClass]
    public class MovieInformationUpdaterTest
    {
        [TestMethod]
        [Ignore] //Side effects
        public void TestUpdate()
        {
            var miu = new MovieInformationUpdater(new MovieSystemService());
            miu.UpdateAllMoviesInDatabase();
        }
    }
}
