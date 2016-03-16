namespace TBag.BloomFilter.Test
{
     using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;
    using TBag.BloomFilters.Estimators;

    /// <summary>
    /// Summary description for BitMinwiseHashEstimatorTest
    /// </summary>
    [TestClass]
    public class BitMinwiseHashEstimatorTest
    {
        public BitMinwiseHashEstimatorTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        /// <summary>
        /// Add items and estimate a difference utilizing a b-bit minwise estimator.
        /// </summary>
        [TestMethod]
        public void BasicFillAndEstimate()
        {
            var data = DataGenerator.Generate().Take(100000).ToList();
            var configuration = new DefaultBloomFilterConfiguration();
            var estimator = new BitMinwiseHashEstimator<TestEntity, long, sbyte>(
               configuration,
               2,
              20,
                (ulong)data.LongCount());
            foreach(var element in data)
            estimator.Add(element);
            data.Modify(2000); 
           var estimator2 = new BitMinwiseHashEstimator<TestEntity,long, sbyte>(configuration, 2, 20, (ulong)data.LongCount());
             foreach(var element in data)
                //just making sure we do not depend upon the order of adding things.
            estimator2.Add(element);
            var differenceCount = data.LongCount() - estimator.Similarity(estimator2) * data.LongCount();
            Assert.IsTrue(differenceCount >= 900 && differenceCount < 2550);
        }
    }
}
