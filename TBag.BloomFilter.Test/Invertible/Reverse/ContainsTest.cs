namespace TBag.BloomFilter.Test.Invertible.Reverse
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using BloomFilters;
    using System.Linq;
    using BloomFilters.Invertible;
    using Infrastructure;
    using System;/// <summary>
                 /// Simple add and lookup test on Bloom filter.
                 /// </summary>
    [TestClass]
    public class ContainsTest
    {
        /// <summary>
        /// Reverse false positive rates are significantly higher (don't use a reverse filter for membership tests).
        /// </summary>
        [TestMethod]
        public void ReverseFalsePositiveTest()
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
            try
            {
                notFoundCount = testData.Count(itm => !bloomFilter.ContainsKey(itm.Id));
                Assert.Fail("Invertible reverse Bloom filter does not support ContainsKey.");
            }
            catch (NotSupportedException) { };
            notFoundCount = DataGenerator.Generate().Skip(addSize).Take(addSize).Count(itm => bloomFilter.Contains(itm));
            Assert.IsTrue(notFoundCount <= 20 * errorRate * addSize, "False positive error rate violated");
            try
            {
                notFoundCount = DataGenerator.Generate().Skip(addSize).Take(addSize).Count(itm => bloomFilter.ContainsKey(itm.Id));
                Assert.Fail("Invertible reverse Bloom filter does not support ContainsKey.");
            }
            catch (NotSupportedException) { };
        }
    }
}
