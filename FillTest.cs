using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.HashAlgorithms;
using System.Collections.Generic;
using TBag.BloomFilters;
using System.Linq;

namespace TBag.BloomFilter.Test
{
    [TestClass]
    public class FillTest
    {
        private IEnumerable<TestEntity> DataGenerator()
        {
            var id = 1L;
            var mers = new MersenneTwister();
            while(id < long.MaxValue)
            {
                yield return new TestEntity { Id = id, Value = mers.NextInt32() };
                id++;
            }
        }

        [TestMethod]
        public void FalsePositiveTest()
        {
            var addSize = 100000;
            var testData = DataGenerator().Take(addSize).ToArray();
           var errorRate = 0.02F;
            var size = testData.Length;
            var configuration = new SingleBucketBloomFilterConfiguration();
            configuration.SplitByHash = true;
            var bloomFilter = new InvertibleBloomFilter<TestEntity, long>(size, errorRate, configuration);
            foreach(var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var notFoundCount = 0;
            foreach(var itm in testData)
            {
                if (!bloomFilter.Contains(itm))
                {
                    notFoundCount++;
                }
            }
            Assert.IsTrue(notFoundCount <= errorRate * size, "False negative error rate violated");
            notFoundCount = 0;
            foreach(var itm in DataGenerator().Skip(addSize).Take(addSize))
            {
                if (bloomFilter.Contains(itm))
                {
                    notFoundCount++;
                }
            }
            Assert.IsTrue(notFoundCount <= errorRate * size, "False positive error rate violated");

        }

        [TestMethod]
        public void SetNoDiffTest()
        {
            var addSize = 100000;
            var testData = DataGenerator().Take(addSize).ToArray();
            var errorRate = 0.02F;
            var size = testData.Length;
            var configuration = new SingleBucketBloomFilterConfiguration();
            configuration.SplitByHash = true;
            var bloomFilter = new InvertibleBloomFilter<TestEntity, long>(size, errorRate, configuration);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            testData[0].Value = -testData[0].Value;
            var secondBloomFilter = new InvertibleBloomFilter<TestEntity, long>(size, errorRate, configuration);
            foreach (var itm in testData)
            {
                secondBloomFilter.Add(itm);
            }
            var changed = new HashSet<long>();
            var onlyInFirst = new HashSet<long>();
            var onlyInSecond = new HashSet<long>();
            bloomFilter
                .Substract(secondBloomFilter, changed);
            bloomFilter
                .Decode(onlyInFirst, onlyInSecond, changed);
            
            Assert.IsTrue(changed.Count != 1, "Incorrect number of changes detected");
            Assert.IsTrue(onlyInFirst.Count == 0, "False positive on only in first");
            Assert.IsTrue(onlyInSecond.Count == 0, "False positive on only in second");
        }
    }
}
