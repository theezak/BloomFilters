using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilters;
using System.Linq;

namespace TBag.BloomFilter.Test
{
    [TestClass]
    public class BloomFilterFoldTest
    {
        [TestMethod]
        public void SimpleFold()
        {
            var addSize = 50;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var size = testData.Length;
            var configuration = new KeyValueBloomFilterConfiguration();
            var bloomFilter = new InvertibleReverseBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(size, 1024, (uint)3);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var folded = bloomFilter.Fold(4);
            Assert.AreEqual(256, folded.Extract().BlockSize);
            Assert.IsTrue(testData.All(item => testData.Contains(item)), "False negative");
        }
    }
}
