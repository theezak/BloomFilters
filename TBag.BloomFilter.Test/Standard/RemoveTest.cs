using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilters.Invertible;
using TBag.BloomFilter.Test.Infrastructure;
using System.Linq;
using TBag.BloomFilters.Standard;

namespace TBag.BloomFilter.Test.Standard
{
    [TestClass]
    public class RemoveTest
    {
        [TestMethod]
        public void BloomFilterRemoveItemTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new BloomFilter<TestEntity, long>(configuration);
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
            //Bloom filter does not behave well under removal
            Assert.AreEqual(containedAfterRemove, 4137, "Wrong item count after removal.");
        }

        [TestMethod]
        public void BloomFilterRemoveKeyTest()
        {
            var addSize = 1000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var errorRate = 0.001F;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new BloomFilter<TestEntity, long>(configuration);
            bloomFilter.Initialize(2 * addSize, errorRate);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var contained = testData.Count(item => bloomFilter.Contains(item));
            foreach (var item in testData.Take(addSize / 2))
            {
                bloomFilter.RemoveKey(item.Id);
            }
            var containedAfterRemove = testData.Count(item => bloomFilter.Contains(item));
            //tricky:Bloom filters do not behave well under removal.
            Assert.AreEqual(containedAfterRemove, 424, "Wrong item count after removal.");
        }
    }
}
