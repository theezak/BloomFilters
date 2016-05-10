using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilter.Test.Infrastructure;
using System.Linq;
using TBag.BloomFilters.Standard;

namespace TBag.BloomFilter.Test.Standard
{
    [TestClass]
    public class AddTest
    {
        /// <summary>
        /// Add two Bloom filters.
        /// </summary>
        [TestMethod]
        public void BloomFilterAddTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var testData2 = DataGenerator.Generate().Skip(addSize).Take(addSize).ToArray();
            var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new BloomFilter<TestEntity,long>(configuration);
            bloomFilter.Initialize(2*size, errorRate);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var bloomFilter2 = new BloomFilter<TestEntity,long>(configuration);
            bloomFilter2.Initialize(2 * size, errorRate);
            foreach (var itm in testData2)
            {
                bloomFilter2.Add(itm);
            }
            bloomFilter.Add(bloomFilter2);
            var contained = testData.Union(testData2).Count(item => bloomFilter.ContainsKey(item.Id));
            Assert.AreEqual(contained, 2 * addSize, "Not all items found in added Bloom filters");
        }

        /// <summary>
        /// Add two Bloom filters of different size.
        /// </summary>
        [TestMethod]
        public void BloomFilterAddDifferentSizesTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var testData2 = DataGenerator.Generate().Skip(addSize).Take(addSize).ToArray();
            var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new BloomFilter<TestEntity,long>(configuration);
            bloomFilter.Initialize(4 * size, errorRate);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var bloomFilter2 = new BloomFilter<TestEntity,long>(configuration);
            //We have to create a foldable version.
            var data = bloomFilter.Extract();
            var foldFactor = configuration.FoldingStrategy.GetAllFoldFactors(data.BlockSize).Where(f=>f>1).OrderBy(f=> f).First();
            bloomFilter2.Initialize(addSize, data.BlockSize / foldFactor,  data.HashFunctionCount);
            foreach (var itm in testData2)
            {
                bloomFilter2.Add(itm);
            }
            bloomFilter.Add(bloomFilter2);
            var contained = testData.Union(testData2).Count(item => bloomFilter.Contains(item));
            Assert.AreEqual(2 * addSize, contained, "Not all items found in added Bloom filters");
        }
    }
}
