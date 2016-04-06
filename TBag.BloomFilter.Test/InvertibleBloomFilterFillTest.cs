

namespace TBag.BloomFilter.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using BloomFilters;
    using System.Linq;
    using BloomFilters.Invertible;
    using Infrastructure;    /// <summary>
                             /// Simple add and lookup test on Bloom filter.
                             /// </summary>
    [TestClass]
    public class InvertibleBloomFilterFillTest
    {
        [TestMethod]
        public void InvertibleBloomFilterFalsePositiveTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(size, errorRate);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var notFoundCount = testData.Count(itm => !bloomFilter.Contains(itm));
            Assert.IsTrue(notFoundCount == 0, "False negative error rate violated");
            notFoundCount = testData.Count(itm => !bloomFilter.ContainsKey(itm.Id));
            Assert.IsTrue(notFoundCount == 0, "False negative error rate violated on ContainsKey");
            notFoundCount = DataGenerator.Generate().Skip(addSize).Take(addSize).Count(itm => bloomFilter.Contains(itm));
            Assert.IsTrue(notFoundCount <= errorRate * addSize, "False positive error rate violated");
            notFoundCount = DataGenerator.Generate().Skip(addSize).Take(addSize).Count(itm => bloomFilter.ContainsKey(itm.Id));
            Assert.IsTrue(notFoundCount <= errorRate * addSize, "False positive error rate violated on ContainsKey");
        }

        /// <summary>
        /// Reverse IBF has worse false-positive rates.
        /// </summary>
        [TestMethod]
        public void ReverseInvertibleBloomFilterFalsePositiveTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new KeyValueBloomFilterConfiguration();
            var bloomFilter = new InvertibleReverseBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(size, errorRate);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var notFoundCount = testData.Count(itm => !bloomFilter.Contains(itm));
            Assert.IsTrue(notFoundCount == 0, "False negative error rate violated");
            notFoundCount = DataGenerator.Generate().Skip(addSize).Take(addSize).Count(itm => bloomFilter.Contains(itm));
            //reverse Bloom filter has a much higher false positive rate, and is thus not a good choice for membership tests.
            Assert.IsTrue(notFoundCount <= 20 * errorRate * addSize, "False positive error rate violated");
        }

        /// <summary>
        /// Hybrid has the same (or better ) false positive rates.
        /// </summary>
        [TestMethod]
        public void HybridInvertibleBloomFilterFalsePositiveTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new HybridDefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleHybridBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(size, errorRate);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var notFoundCount = testData.Count(itm => !bloomFilter.Contains(itm));
            Assert.IsTrue(notFoundCount == 0, "False negative error rate violated");
            notFoundCount = testData.Count(itm => !bloomFilter.ContainsKey(itm.Id));
            Assert.IsTrue(notFoundCount == 0, "False negative error rate violated on ContainsKey");
            notFoundCount = DataGenerator.Generate().Skip(addSize).Take(addSize).Count(itm => bloomFilter.Contains(itm));
            Assert.IsTrue(notFoundCount <= errorRate * addSize, "False positive error rate violated");
            notFoundCount = DataGenerator.Generate().Skip(addSize).Take(addSize).Count(itm => bloomFilter.ContainsKey(itm.Id));
            Assert.IsTrue(notFoundCount <= errorRate * addSize, "False positive error rate violated on ContainsKey");
        }

        [TestMethod]
        public void InvertibleBloomFilterSetDiffTest()
        {
            var addSize = 1000;
            var modCount = 50;
            var dataSet1 = DataGenerator.Generate().Take(addSize).ToList();
            var dataSet2 = DataGenerator.Generate().Take(addSize).ToList();
            dataSet2.Modify(modCount);
            var configuration = new KeyValueBloomFilterConfiguration();
            var bloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration, false);
            bloomFilter.Initialize(2 * modCount, 0.0001F);
            foreach (var itm in dataSet1)
            {
                bloomFilter.Add(itm);
            }
            var secondBloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration, false);
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
            var allFound = changed.Union(onlyInFirst).Union(onlyInSecond).OrderBy(i => i).ToArray();
            var onlyInSet1 = dataSet1.Where(d => dataSet2.All(d2 => d2.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
            var onlyInSet2 = dataSet2.Where(d => dataSet1.All(d1 => d1.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
            var modified = dataSet1.Where(d => dataSet2.Any(d2 => d2.Id == d.Id && d2.Value != d.Value)).Select(d => d.Id).OrderBy(id => id).ToArray();
            var allModified = onlyInSet1.Union(onlyInSet2).Union(modified).OrderBy(i => i).ToArray();
            Assert.IsTrue(decoded == true, "Decoding failed");
            Assert.IsTrue(allModified.Count() - allFound.Count() <= 2 ,
                "Number of missed changes across the sets exceeded 2");
        }

        [TestMethod]
        public void ReverseInvertibleBloomFilterSetDiffTest()
        {
            var addSize = 1000;
            var modCount = 20;
            var dataSet1 = DataGenerator.Generate().Take(addSize).ToList();
            var dataSet2 = DataGenerator.Generate().Take(addSize).ToList();
            dataSet2.Modify(modCount);
            var configuration = new KeyValueBloomFilterConfiguration();
            var bloomFilter = new InvertibleReverseBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(2 * modCount, 0.000001F);
            foreach (var itm in dataSet1)
            {
                bloomFilter.Add(itm);
            }
            var secondBloomFilter = new InvertibleReverseBloomFilter<TestEntity, long, sbyte>(configuration);
            secondBloomFilter.Initialize(2 * modCount, 0.000001F);
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
            Assert.IsTrue(decoded == true, "Decoding failed"); 
            Assert.IsTrue(onlyInSet1.Length == onlyInFirst.Count, "Incorrect number of changes detected");
            Assert.IsTrue(onlyInSet2.Length == onlyInSecond.Count, "False positive on only in first");
            Assert.IsTrue(changed.Count == modified.Length, "False positive on only in second");
        }
    }
}
