namespace TBag.BloomFilter.Test.Invertible.Standard
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;
    using TBag.BloomFilters.Invertible;
    using TBag.BloomFilter.Test.Infrastructure;

    [TestClass]
    public class FoldTest
    {
        [TestMethod]
        public void StandardSimpleFold()
        {
            var addSize = 50;
            var testData = DataGenerator.Generate().Take(addSize).ToArray();
            var size = testData.Length;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize(size, 1024, (uint)3);
            foreach (var itm in testData)
            {
                bloomFilter.Add(itm);
            }
            var positiveCount = DataGenerator.Generate().Take(500).Count(itm => bloomFilter.Contains(itm));
            var folded = bloomFilter.Fold(4);
            var positiveCountAfterFold = DataGenerator.Generate().Take(500).Count(itm => bloomFilter.Contains(itm));
            Assert.AreEqual(positiveCount, positiveCountAfterFold, "False positive count different after fold");
            Assert.AreEqual(256, folded.Extract().BlockSize);
            Assert.IsTrue(testData.All(item => testData.Contains(item)), "False negative found");
        }
    }
}
