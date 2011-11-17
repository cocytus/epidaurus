using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Epidaurus.ScannerLib.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Scanner
{
    [TestClass]
    public class TestGoogleApi
    {
        [TestMethod]
        public void CanFindTheMatrix()
        {
            var results = GoogleApi.Search("the matrix 1999 site:imdb.com");
            Assert.IsNotNull(results);
            Assert.IsTrue(results.Length > 0);
            Assert.IsTrue(results[0].Url == "http://www.imdb.com/title/tt0133093/");
        }
    }
}
