namespace TBag.BloomFilter.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;
    using BloomFilters.Estimators;
    using BloomFilters.Invertible.Estimators;
    /// <summary>
    /// Summary description for hybridEstimatorTest
    /// </summary>
    [TestClass]
    public class HybridEstimatorTest
    {
        [TestMethod]
        public void HybridEstimatorBasicFillAndEstimate()
        {
            var data = DataGenerator.Generate().Take(10000).ToArray();
            var configuration = new KeyValueLargeBloomFilterConfiguration();
            var factory = new HybridEstimatorFactory();
            var estimator = factory.Create(configuration, data.Length);
            foreach (var element in data)
                estimator.Add(element);
            var estimator2 = factory.Create(configuration, data.Length);
            var halfTheDiff = 100;
            foreach (var elt in data.Take(halfTheDiff))
            {
                elt.Id += 1000000;
            }
            foreach (var elt in data.Skip(100000).Take(halfTheDiff))
            {
                elt.Value += 10;
            }
            foreach (var element in data)
                //just making sure we do not depend upon the order of adding things.
                estimator2.Add(element);
            var differenceCount = estimator.Decode(estimator2);
            Assert.IsTrue(differenceCount >=  (2*halfTheDiff), "Estimate below the difference count.");
        }
    }
}
