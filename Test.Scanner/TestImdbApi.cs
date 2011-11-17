using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Epidaurus.ScannerLib.Imdb;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Scanner
{
    [TestClass]
    public class TestImdbApi
    {
        [TestMethod]
        public void CanFindFlåklypa()
        {
            var id = ImdbApi.ImdbIdFinder("flåklypa grand prix");
            Assert.AreEqual(id, "tt0073000");
        }

        [TestMethod]
        public void CanFindTheMatrix()
        {
            var id = ImdbApi.ImdbIdFinder("the matrix", 1999);
            Assert.AreEqual(id, "tt0133093");
        }
    }
}
