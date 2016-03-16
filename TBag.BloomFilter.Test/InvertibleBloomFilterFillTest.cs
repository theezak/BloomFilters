

namespace TBag.BloomFilter.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using BloomFilters;
    using System.Linq;

    /// <summary>
    /// Simple add and lookup test on Bloom filter.
    /// </summary>
    [TestClass]
    public class InvertibleBloomFilterFillTest
    {
        [TestMethod]
        public void InvertibleBloomFilterFalsePositiveTest()
        {
            var addSize = 100000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
           var errorRate = 0.02F;
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(size, errorRate, configuration);
            foreach(var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var notFoundCount = testData.Count(itm => !bloomFilter.Contains(itm));
            Assert.IsTrue(notFoundCount <= errorRate * size, "False negative error rate violated");
            notFoundCount = DataGenerator.Generate().Skip(addSize).Take(addSize).Count(itm => bloomFilter.Contains(itm));
            Assert.IsTrue(notFoundCount <= errorRate * size, "False positive error rate violated");

        }

        [TestMethod]
        public void ReverseInvertibleBloomFilterFalsePositiveTest()
        {
            var addSize = 100000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var errorRate = 0.02F;
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleReverseBloomFilter<TestEntity, long, sbyte>(size, errorRate, configuration);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var notFoundCount = testData.Count(itm => !bloomFilter.Contains(itm));
            Assert.IsTrue(notFoundCount <= errorRate * size, "False negative error rate violated");
            notFoundCount = DataGenerator.Generate().Skip(addSize).Take(addSize).Count(itm => bloomFilter.Contains(itm));
            Assert.IsTrue(notFoundCount <= errorRate * size, "False positive error rate violated");
        }

        [TestMethod]
        public void HybridInvertibleBloomFilterFalsePositiveTest()
        {
            var addSize = 100000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var errorRate = 0.02F;
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleHybridBloomFilter<TestEntity, long, sbyte>(size, errorRate, configuration);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var notFoundCount = testData.Count(itm => !bloomFilter.Contains(itm));
            Assert.IsTrue(notFoundCount <= errorRate * size, "False negative error rate violated");
            notFoundCount = DataGenerator.Generate().Skip(addSize).Take(addSize).Count(itm => bloomFilter.Contains(itm));
            Assert.IsTrue(notFoundCount <= errorRate * size, "False positive error rate violated");
        }

        [TestMethod]
        public void InvertibleBloomFilterSetDiffTest()
        {
            var addSize = 100000;
            var testData = DataGenerator.Generate().Take(addSize).ToList();
            var size = testData.LongCount();
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(size, 0.01F, configuration);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            //make one change
           testData.Modify(50);
            var secondBloomFilter = new InvertibleBloomFilter<TestEntity, long,sbyte>(size, 0.01F, configuration);
            foreach (var itm in testData)
            {
                secondBloomFilter.Add(itm);
            }
            var changed = new HashSet<long>();
            var onlyInFirst = new HashSet<long>();
            var onlyInSecond = new HashSet<long>();
            var decoded = bloomFilter
                .SubtractAndDecode(secondBloomFilter, onlyInFirst, onlyInSecond, changed);
            
            Assert.IsTrue(changed.Count == 25, "Incorrect number of changes detected");
            Assert.IsTrue(onlyInFirst.Count == 0, "False positive on only in first");
            Assert.IsTrue(onlyInSecond.Count == 0, "False positive on only in second");
        }

        [TestMethod]
        public void ReverseInvertibleBloomFilterSetDiffTest()
        {
            var addSize = 100000;
            var testData = DataGenerator.Generate().Take(addSize).ToList();
            long size = testData.LongCount();
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleReverseBloomFilter<TestEntity, long, sbyte>(size, 0.01F, configuration);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            //make one change
            testData.Modify(50);
            var secondBloomFilter = new InvertibleReverseBloomFilter<TestEntity, long, sbyte>(size, 0.01F, configuration);
            foreach (var itm in testData)
            {
                secondBloomFilter.Add(itm);
            }
            var changed = new HashSet<long>();
            var onlyInFirst = new HashSet<long>();
            var onlyInSecond = new HashSet<long>();
            var decoded = bloomFilter
                .SubtractAndDecode(secondBloomFilter, onlyInFirst, onlyInSecond, changed);

            Assert.IsTrue(changed.Count == 25, "Incorrect number of changes detected");
            Assert.IsTrue(onlyInFirst.Count == 0, "False positive on only in first");
            Assert.IsTrue(onlyInSecond.Count == 0, "False positive on only in second");
        }

        [TestMethod]
        public void HybridInvertibleBloomFilterSetDiffTest()
        {
            var addSize = 100000;
            var testData = DataGenerator.Generate().Take(addSize).ToList();
            long size = testData.LongCount();
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleHybridBloomFilter<TestEntity, long, sbyte>(size, 0.01F, configuration);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            //make one change
            testData.Modify(50);
            var secondBloomFilter = new InvertibleHybridBloomFilter<TestEntity, long, sbyte>(size, 0.01F, configuration);
            foreach (var itm in testData)
            {
                secondBloomFilter.Add(itm);
            }
            var changed = new HashSet<long>();
            var onlyInFirst = new HashSet<long>();
            var onlyInSecond = new HashSet<long>();
            var decoded = bloomFilter
                .SubtractAndDecode(secondBloomFilter, onlyInFirst, onlyInSecond, changed);

            Assert.IsTrue(changed.Count == 25, "Incorrect number of changes detected");
            Assert.IsTrue(onlyInFirst.Count == 0, "False positive on only in first");
            Assert.IsTrue(onlyInSecond.Count == 0, "False positive on only in second");
        }
    }
}
