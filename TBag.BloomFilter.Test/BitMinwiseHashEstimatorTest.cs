namespace TBag.BloomFilter.Test
{
     using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;
    using BloomFilters.Estimators;

    /// <summary>
    /// Summary description for BitMinwiseHashEstimatorTest
    /// </summary>
    [TestClass]
    public class BitMinwiseHashEstimatorTest
    {
        /// <summary>
        /// Add items and estimate a difference utilizing a b-bit minwise estimator.
        /// </summary>
        [TestMethod]
        public void BasicFillAndEstimate()
        {
            var data = DataGenerator.Generate().Take(100000).ToList();
            var data2 = DataGenerator.Generate().Take(100000).ToList();
            data.Modify(10000);
            var configuration = new DefaultBloomFilterConfiguration();
            var estimator = new BitMinwiseHashEstimator<TestEntity, long, sbyte>(
               configuration,
               2,
              20,
                (ulong)data.LongCount());
            foreach(var element in data)
            estimator.Add(element);
          
           var estimator2 = new BitMinwiseHashEstimator<TestEntity,long, sbyte>(configuration, 2, 20, (ulong)data.LongCount());
             foreach(var element in data2)
                //just making sure we do not depend upon the order of adding things.
            estimator2.Add(element);
            var differenceCount = data.LongCount() - estimator.Similarity(estimator2) * data.LongCount();
            Assert.IsTrue(differenceCount >= 0.45 * 10000);
        }
    }
}
