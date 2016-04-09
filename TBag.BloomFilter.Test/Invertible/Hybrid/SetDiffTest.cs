using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilter.Test.Infrastructure;
using System.Collections.Generic;
using TBag.BloomFilters.Invertible;
using System.Linq;

namespace TBag.BloomFilter.Test.Invertible.Hybrid
{
    [TestClass]
    public class SetDiffTest
    {
        /// <summary>
        /// test between two sets.
        /// </summary>
        [TestMethod]
        public void HybridInvertibleBloomFilterSetDiffTest()
        {
            var addSize = 1000;
            var modCount = 50;
            var dataSet1 = DataGenerator.Generate().Take(addSize).ToList();
            var dataSet2 = DataGenerator.Generate().Take(addSize).ToList();
            dataSet2.Modify(modCount);
            var configuration = new HybridDefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleHybridBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(2 * modCount, 0.0001F);
            foreach (var itm in dataSet1)
            {
                bloomFilter.Add(itm);
            }
            var secondBloomFilter = new InvertibleHybridBloomFilter<TestEntity, long, sbyte>(configuration);
            secondBloomFilter.Initialize(2 * modCount, 0.0001F);
            foreach (var itm in dataSet2)
            {
                secondBloomFilter.Add(itm);
            }
            var changed = new HashSet<long>();
            var onlyInFirst = new HashSet<long>();
            var onlyInSecond = new HashSet<long>();
            var decoded = bloomFilter
                .SubtractAndDecode(secondBloomFilter, onlyInFirst, onlyInSecond, changed);
            var onlyInSet1 = dataSet1.Where(d => dataSet2.All(d2 => d2.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
            var onlyInSet2 = dataSet2.Where(d => dataSet1.All(d1 => d1.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
            var modified = dataSet1.Where(d => dataSet2.Any(d2 => d2.Id == d.Id && d2.Value != d.Value)).Select(d => d.Id).OrderBy(id => id).ToArray();
            Assert.IsTrue(decoded??false, "Decoding failed");
            Assert.IsTrue(onlyInSet1.Length == onlyInFirst.Count, "Incorrect number of changes detected");
            Assert.IsTrue(onlyInSet2.Length == onlyInSecond.Count, "False positive on only in first");
            Assert.IsTrue(changed.Count == modified.Length, "False positive on only in second");
        }

        /// <summary>
        /// Test with one empty set.
        /// </summary>
        [TestMethod]
        public void HybridInvertibleBloomFilterEmptySetDiffTest()
        {
            var addSize = 1000;
           var dataSet1 = DataGenerator.Generate().Take(0).ToList();
            var dataSet2 = DataGenerator.Generate().Take(addSize).ToList();
            var configuration = new HybridDefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleHybridBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(2 * addSize, 0.0001F);
            foreach (var itm in dataSet1)
            {
                bloomFilter.Add(itm);
            }
            var secondBloomFilter = new InvertibleHybridBloomFilter<TestEntity, long, sbyte>(configuration);
            secondBloomFilter.Initialize(2 * addSize, 0.0001F);
            foreach (var itm in dataSet2)
            {
                secondBloomFilter.Add(itm);
            }
            var changed = new HashSet<long>();
            var onlyInFirst = new HashSet<long>();
            var onlyInSecond = new HashSet<long>();
            var decoded = bloomFilter
                .SubtractAndDecode(secondBloomFilter, onlyInFirst, onlyInSecond, changed);
            var onlyInSet1 = dataSet1.Where(d => dataSet2.All(d2 => d2.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
            var onlyInSet2 = dataSet2.Where(d => dataSet1.All(d1 => d1.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
            var modified = dataSet1.Where(d => dataSet2.Any(d2 => d2.Id == d.Id && d2.Value != d.Value)).Select(d => d.Id).OrderBy(id => id).ToArray();
            Assert.IsTrue(decoded == true, "Decoding failed");
            Assert.IsTrue(onlyInSet1.Length == onlyInFirst.Count, "Incorrect number of changes detected");
            Assert.IsTrue(onlyInSet2.Length == onlyInSecond.Count, "False positive on only in first");
            Assert.IsTrue(changed.Count == modified.Length, "False positive on only in second");
        }
    }
}
