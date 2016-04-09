using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilter.Test.Infrastructure;
using System.Linq;
using TBag.BloomFilters.Invertible;

namespace TBag.BloomFilter.Test.Invertible.Reverse
{
    [TestClass]
    public class CompressTest
    {
        /// <summary>
        /// Test reverse filter compression
        /// </summary>
        /// <remarks>A reverse filter has signficantly worse false positive rate on membership tests, so don't use it for that.</remarks>
        [TestMethod]
        public void ReverseCompressTest()
        {
            var addSize = 10000;
            var errorRate = 0.001F;
            var data = DataGenerator.Generate().Take(addSize).ToArray();
            var filter = new InvertibleReverseBloomFilter<TestEntity,long,sbyte>(new KeyValueBloomFilterConfiguration());
            filter.Initialize(50 * data.Length, errorRate);
            Assert.AreEqual(filter.Capacity, 500000, "Unexpected size of reverse Bloom filter.");
            foreach(var item in data)
            {
                filter.Add(item);
            }
            //check error rate.
            var notFoundCount = DataGenerator.Generate().Skip(addSize).Take(10000).Count(itm => filter.Contains(itm));
            Assert.IsTrue(notFoundCount <= 4 * errorRate * addSize, "Uncompressed reverse Bloom filter exceeded error rate.");
            filter.Compress(true);
            Assert.AreEqual(filter.Capacity, 15151, "Unexpected size of compressed reverse Bloom filter.");
            var compressNotFoundCount = DataGenerator.Generate().Skip(addSize).Take(10000).Count(itm => filter.Contains(itm));
            Assert.IsTrue(compressNotFoundCount <= 4 * errorRate * addSize, "Compressed reverse Bloom filter exceeded error rate.");
        }
    }
}
