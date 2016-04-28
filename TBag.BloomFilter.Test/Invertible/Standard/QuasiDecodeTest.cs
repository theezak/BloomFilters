using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilter.Test.Infrastructure;
using System.Linq;
using TBag.BloomFilters.Invertible;

namespace TBag.BloomFilter.Test.Invertible.Standard
{
    [TestClass]
    public class QuasiDecodeTest
    {
        [TestMethod]
        public void InvertibleBloomFilterQuasiDecodeTest()
        {
            var size = 100000;
            var data = DataGenerator.Generate().Take(size).ToList();
            var errorRate = 0.001F;
            var configuration = new DefaultBloomFilterConfiguration();
            var bloomFilter = new InvertibleBloomFilter<TestEntity, long, sbyte>(configuration);
            bloomFilter.Initialize( size, errorRate);
            foreach (var itm in data)
            {
                bloomFilter.Add(itm);
            }
            data = DataGenerator.Generate().Skip(500).Take(8000).ToList();
            data.Modify(1000);
            var estimate = bloomFilter.QuasiDecode(data);
            //actual difference is expected to be about 91500
            Assert.IsTrue(estimate > 90500 && estimate < 97000, "Unexpected estimate for difference.");
        }
    }
}
