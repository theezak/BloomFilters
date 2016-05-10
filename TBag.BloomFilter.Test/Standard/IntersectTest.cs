using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilter.Test.Infrastructure;
using TBag.BloomFilters.Invertible;
using System.Linq;
using TBag.BloomFilters.Standard;

namespace TBag.BloomFilter.Test.Standard
{
    /// <summary>
    /// Summary description for IntersectTest
    /// </summary>
    [TestClass]
    public class IntersectTest
    { 
       /// <summary>
       /// Simple intersect between two equal Bloom filters.
       /// </summary>
        [TestMethod]
        public void BloomFilterIntersectEqualFiltersTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
             var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new BloomFilter<TestEntity,long>(configuration);
            bloomFilter.Initialize(2 * size, errorRate);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var bloomFilter2 = new BloomFilter<TestEntity, long>(configuration);
            bloomFilter2.Initialize(2 * size, errorRate);
            foreach (var itm in testData)
            {
                bloomFilter2.Add(itm);
            }
            bloomFilter.Intersect(bloomFilter2);
            //item count will be off due to estimated size.
            Assert.IsTrue(bloomFilter.ItemCount >= addSize);
            Assert.IsTrue(testData.All(bloomFilter.Contains));
        }

        [TestMethod]
        public void InvertibleIntersectDifferentFiltersTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(2 * size, errorRate);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var bloomFilter2 = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter2.Initialize(2 * size, errorRate);
            foreach (var itm in testData.Skip(1000))
            {
                bloomFilter2.Add(itm);
            }
            bloomFilter.Intersect(bloomFilter2);
            Assert.AreEqual(9000, bloomFilter.ItemCount);
            var count = testData.Skip(1000).Count(bloomFilter.Contains);
            //Note: intersect introduces a horrible error rate when utilizing XOR, so don't actually use intersect.
            //There are however definitions of operations possible where the intersect would not have this horrible effect.
            Assert.IsTrue(count > 7800);
        }
    }
}
