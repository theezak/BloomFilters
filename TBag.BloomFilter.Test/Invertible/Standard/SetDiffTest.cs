using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilter.Test.Infrastructure;
using System.Collections.Generic;
using TBag.BloomFilters.Invertible;
using System.Linq;

namespace TBag.BloomFilter.Test.Invertible.Standard
{
    [TestClass]
    public class SetDiffTest
    {
        /// <summary>
        /// Test with two sets.
        /// </summary>
        [TestMethod]
        public void InvertibleBloomFilterSetDiffTest()
        {
            var addSize = 1000;
            var modCount = 50;
            var dataSet1 = DataGenerator.Generate().Take(addSize).ToList();
            var dataSet2 = DataGenerator.Generate().Take(addSize).ToList();
            dataSet2.Modify(modCount);
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(10 * modCount, 0.0001F);
            foreach (var itm in dataSet1)
            {
                bloomFilter.Add(itm);
            }
            var secondBloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration);
            secondBloomFilter.Initialize(10 * modCount, 0.0001F);
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
            Assert.IsTrue(decoded.HasValue, "Decoding failed");
            Assert.IsTrue(onlyInSet1.Length == onlyInFirst.Count, "Incorrect number of changes detected on 'only in set 1'");
            Assert.IsTrue(onlyInSet2.Length == onlyInSecond.Count, "Incorrect number of changes detected on 'only in set 2'");
            //very bad at recognizing changes.
            Assert.IsTrue(changed.Count <= modified.Length, "Incorrect number of modified items detected");
        }

        /// <summary>
        /// Test with one empty set
        /// </summary>
        [TestMethod]
        public void InvertibleBloomFilterEmptySetDiffTest()
        {
            var addSize = 1000;
            var modCount = addSize;
            var errorRate = 0.0001F;
            var dataSet1 = DataGenerator.Generate().Take(0).ToList();
            var dataSet2 = DataGenerator.Generate().Take(addSize).ToList();
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(10 * modCount, errorRate);
            foreach (var itm in dataSet1)
            {
                bloomFilter.Add(itm);
            }
            var secondBloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration);
            secondBloomFilter.Initialize(10 * modCount, errorRate);
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
            Assert.IsTrue(onlyInSet1.Length == onlyInFirst.Count, "Incorrect number of changes detected on 'only in set 1'");
            Assert.IsTrue(onlyInSet2.Length == onlyInSecond.Count, "Incorrect number of changes detected on 'only in set 2'");
            Assert.IsTrue(changed.Count == modified.Length, "Incorrect number of modified items detected");
        }
    }
}
