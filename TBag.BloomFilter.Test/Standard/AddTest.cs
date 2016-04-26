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
        [TestMethod]
        public void BloomFilterAddTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var testData2 = DataGenerator.Generate().Skip(addSize).Take(addSize).ToArray();
            var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new BloomFilter<long>(configuration);
            bloomFilter.Initialize(2*size, errorRate);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm.Id);
            }
            var bloomFilter2 = new BloomFilter<long>(configuration);
            bloomFilter2.Initialize(2 * size, errorRate);
            foreach (var itm in testData2)
            {
                bloomFilter2.Add(itm.Id);
            }
            bloomFilter.Add(bloomFilter2);
            var contained = testData.Union(testData2).Count(item => bloomFilter.Contains(item.Id));
            Assert.AreEqual(contained, 2 * addSize, "Not all items found in added Bloom filters");
        }

        [TestMethod]
        public void BloomFilterAddDifferentSizesTest()
        {
            var addSize = 10000;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var testData2 = DataGenerator.Generate().Skip(addSize).Take(addSize).ToArray();
            var errorRate = 0.001F;
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new BloomFilter<long>(configuration);
            bloomFilter.Initialize(4 * size, errorRate);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm.Id);
            }
            var bloomFilter2 = new BloomFilter<long>(configuration);
            //We have to create a foldable version.
            var data = bloomFilter.Extract();
            var foldFactor = configuration.FoldingStrategy.GetAllFoldFactors(data.BlockSize).Where(f=>f>1).OrderBy(f=> f).First();
            bloomFilter2.Initialize(addSize, data.BlockSize / foldFactor,  data.HashFunctionCount);
            foreach (var itm in testData2)
            {
                bloomFilter2.Add(itm.Id);
            }
            bloomFilter.Add(bloomFilter2);
            var contained = testData.Union(testData2).Count(item => bloomFilter.Contains(item.Id));
            Assert.AreEqual(2 * addSize, contained, "Not all items found in added Bloom filters");
        }
    }
}
