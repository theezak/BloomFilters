namespace TBag.BloomFilter.Test.Invertible.Hybrid
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;
    using BloomFilters.Invertible;
    using Infrastructure;

    /// <summary>
    /// Test for folding a hybrid invertible Bloom filter.
    /// </summary>
    [TestClass]
    public class FoldTest
    {
        [TestMethod]
        public void HybridSimpleFold()
        {
            var addSize = 50;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var size = testData.Length;
            var configuration = new HybridDefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleHybridBloomFilter<TestEntity, long, sbyte>(configuration);
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
            Assert.IsTrue(testData.All(item => testData.Contains(item)), "False negative found");
        }
    }
}
