using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilters.Invertible;
using TBag.BloomFilter.Test.Infrastructure;
using System.Linq;

namespace TBag.BloomFilter.Test.Invertible.Reverse
{
    [TestClass]
    public class RemoveTest
    {
        [TestMethod]
        public void ReverseRemoveItemTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new KeyValueBloomFilterConfiguration();
            var bloomFilter = new InvertibleReverseBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(2*size, errorRate);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var contained = testData.Count(item => bloomFilter.Contains(item));
            foreach(var item in testData.Take(addSize / 2))
            {
                bloomFilter.Remove(item);
            }
            var containedAfterRemove = testData.Count(item => bloomFilter.Contains(item));
            //tricky: assuming zero false positives.
            Assert.AreEqual(contained, containedAfterRemove*2, "Wrong item count after removal.");
        }

        [TestMethod]
        public void ReverseRemoveKeyTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new KeyValueBloomFilterConfiguration();
            var bloomFilter = new InvertibleReverseBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(2 * size, errorRate);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var contained = testData.Count(item => bloomFilter.Contains(item));
            try
            {
                foreach (var item in testData.Take(addSize / 2))
                {
                    bloomFilter.RemoveKey(item.Id);
                }
                Assert.Fail("RemoveKey should not be supported by a reverse invertible Bloom filter");
            }
            catch(NotSupportedException)
            { };
        }
    }
}
