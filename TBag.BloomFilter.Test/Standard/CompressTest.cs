using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilter.Test.Infrastructure;
using System.Linq;
using TBag.BloomFilters.Invertible;
using TBag.BloomFilters.Standard;

namespace TBag.BloomFilter.Test.Standard
{
    [TestClass]
    public class CompressTest
    {
        /// <summary>
        /// Test Bloom filter compression
        /// </summary>
          [TestMethod]
        public void StandardCompressTest()
        {
            var addSize = 10000;
            var errorRate = 0.001F;
            var data = DataGenerator.Generate().Take(addSize).ToArray();
            var filter = new BloomFilter<TestEntity,long>(new DefaultBloomFilterConfiguration());
            filter.Initialize(data.Length, errorRate);
            foreach (var item in data)
            {
                filter.Add(item);
            }
            var basecount = DataGenerator
                .Generate()
                .Skip(addSize)
                .Take(addSize)
                .Count(itm => filter.ContainsKey(itm.Id));
            Assert.IsTrue(basecount <= errorRate * addSize);
            filter.Initialize(50* data.Length, errorRate);
           Assert.AreEqual(filter.Capacity, 500000, "Unexpected size of reverse Bloom filter.");
            foreach(var item in data)
            {
                filter.Add(item);
            }
            //check error rate.
            var notFoundCount = DataGenerator.Generate().Skip(addSize).Take(addSize).Count(itm => filter.Contains(itm));
            Assert.IsTrue(notFoundCount <= basecount, "Uncompressed Bloom filter exceeded error rate.");
            Assert.IsTrue(data.All(d => filter.ContainsKey(d.Id)), "False negatives found in uncompressed filter");
            filter = filter.Compress(true);
            Assert.AreEqual(filter.Capacity, 21739, "Unexpected size of compressed Bloom filter.");
            var compressNotFoundCount = DataGenerator.Generate().Skip(addSize).Take(addSize).Count(itm => filter.ContainsKey(itm.Id));
            Assert.IsTrue(data.All(d => filter.ContainsKey(d.Id)), "False negatives found in compressed filter");
            Assert.IsTrue(compressNotFoundCount <= basecount, "Compressed Bloom filter exceeded error rate.");
        }
    }
}
