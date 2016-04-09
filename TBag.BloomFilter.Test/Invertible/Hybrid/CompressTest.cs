using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilter.Test.Infrastructure;
using System.Linq;
using TBag.BloomFilters.Invertible;

namespace TBag.BloomFilter.Test.Invertible.Hybrid
{
    [TestClass]
    public class CompressTest
    {
        [TestMethod]
        public void HybridCompressTest()
        {
            var addSize = 10000;
            var errorRate = 0.001F;
            var data = DataGenerator.Generate().Take(addSize).ToArray();
            var hybridFilter = new InvertibleHybridBloomFilter<TestEntity,long,sbyte>(new HybridDefaultBloomFilterConfiguration());
            hybridFilter.Initialize(50 * data.Length, errorRate);
            Assert.AreEqual(hybridFilter.Capacity, 500000, "Unexpected size of hybrid Bloom filter.");
            foreach(var item in data)
            {
                hybridFilter.Add(item);
            }
            //check error rate.
            var notFoundCount = DataGenerator.Generate().Skip(addSize).Take(10000).Count(itm => hybridFilter.Contains(itm));
            Assert.IsTrue(notFoundCount <= errorRate * addSize, "Uncompressed hybrid Bloom filter exceeded error rate.");
            hybridFilter.Compress(true);
            Assert.AreEqual(hybridFilter.Capacity, 15151, "Unexpected size of compressed hybrid Bloom filter.");
            var compressNotFoundCount = DataGenerator.Generate().Skip(addSize).Take(10000).Count(itm => hybridFilter.Contains(itm));
            Assert.IsTrue(compressNotFoundCount <= errorRate * addSize, "Compressed hybrid Bloom filter exceeded error rate.");
        }
    }
}
