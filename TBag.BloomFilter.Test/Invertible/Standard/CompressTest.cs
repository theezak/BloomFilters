using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilter.Test.Infrastructure;
using System.Linq;
using TBag.BloomFilters.Invertible;

namespace TBag.BloomFilter.Test.Invertible.Standard
{
    /// <summary>
    /// Compresion test on a regular Bloom filter.
    /// </summary>
    [TestClass]
    public class CompressTest
    {
        [TestMethod]
        public void HybridCompressTest()
        {
            var addSize = 10000;
            var errorRate = 0.001F;
            var data = DataGenerator.Generate().Take(addSize).ToArray();
            var filter = new InvertibleBloomFilter<TestEntity,long,sbyte>(new DefaultBloomFilterConfiguration());
            filter.Initialize(50 * data.Length, errorRate);
            Assert.AreEqual(filter.Capacity, 500000, "Unexpected size of Bloom filter.");
            foreach(var item in data)
            {
                filter.Add(item);
            }
            //check error rate.
            var notFoundCount = DataGenerator.Generate().Skip(addSize).Take(10000).Count(itm => filter.Contains(itm));
            Assert.IsTrue(notFoundCount <= errorRate * addSize, "Uncompressed Bloom filter exceeded error rate.");
            filter.Compress(true);
            Assert.AreEqual(filter.Capacity, 15151, "Unexpected size of compressed Bloom filter.");
            var compressNotFoundCount = DataGenerator.Generate().Skip(addSize).Take(10000).Count(itm => filter.Contains(itm));
            Assert.IsTrue(compressNotFoundCount <= errorRate * addSize, "Compressed Bloom filter exceeded error rate.");
        }
    }
}
