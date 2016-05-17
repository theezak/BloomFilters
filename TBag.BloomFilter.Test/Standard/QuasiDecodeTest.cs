namespace TBag.BloomFilter.Test.Standard
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TBag.BloomFilter.Test.Infrastructure;
    using System.Linq;
    using TBag.BloomFilters.Invertible;
    using TBag.BloomFilters.Standard;

    [TestClass]
    public class QuasiDecodeTest
    {
        [TestMethod]
        public void BloomFilterQuasiDecodeTest()
        {
            var size = 100000;
            var data = DataGenerator.Generate().Take(size).ToList();
            var errorRate = 0.001F;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new BloomFilter<TestEntity, long>(configuration);
            bloomFilter.Initialize( size, errorRate);
            foreach (var itm in data)
            {
                bloomFilter.Add(itm);
            }
            data = DataGenerator.Generate().Skip(500).Take(8000).ToList();
            data.Modify(1000);
            var estimate = bloomFilter.QuasiDecode(data);
            Assert.IsTrue(estimate > 90500 && estimate < 95000, "Unexpected estimate for difference.");
        }
    }
}
