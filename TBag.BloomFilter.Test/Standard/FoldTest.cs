namespace TBag.BloomFilter.Test.Standard
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;
    using BloomFilters.Invertible;
    using Infrastructure;
    using BloomFilters.Standard;
    /// <summary>
    /// Test for folding a  Bloom filter.
    /// </summary>
    [TestClass]
    public class FoldTest
    {
        [TestMethod]
        public void BloomFilterSimpleFold()
        {
            var addSize = 50;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new BloomFilter<TestEntity,long>(configuration);
            bloomFilter.Initialize(size, 1024, (uint)3);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var positiveCount = DataGenerator.Generate().Take(500).Count(itm => bloomFilter.Contains(itm));
            var folded = bloomFilter.Fold(4);
            var positiveCountAfterFold = DataGenerator.Generate().Take(500).Count(itm => bloomFilter.Contains(itm));
            Assert.AreEqual(positiveCount, positiveCountAfterFold, "False positive count different after fold");
            Assert.AreEqual(256, folded.BlockSize, "Folded block size is unexpected.");
            Assert.IsTrue(testData.All(bloomFilter.Contains), "False negative found");
        }
    }
}
