using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilters;

namespace TBag.BloomFilter.Test
{
    [TestClass]
    public class IntCompressionTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var array = new int[] { 0, 1, 2, 2, 1, 3, 5, 1, 6, 10, 12 , 10, 11 };
            var compressed = array.Compress();
            var decompressed = compressed.Decompress();
        }
    }
}
