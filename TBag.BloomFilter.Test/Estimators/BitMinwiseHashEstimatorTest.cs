namespace TBag.BloomFilter.Test.Estimators
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;
    using BloomFilters.Estimators;
    using Infrastructure;
    /// <summary>
    /// 
    /// Summary description for BitMinwiseHashEstimatorTest
    /// </summary>
    [TestClass]
    public class BitMinwiseHashEstimatorTest
    {
        /// <summary>
        /// Add two estimators.
        /// </summary>
        [TestMethod]
        public void BitMinwiseAdd()
        {
        }

        /// <summary>
        /// Add items and estimate a difference utilizing a b-bit minwise estimator.
        /// </summary>
        [TestMethod]
        public void BasicFillAndEstimate()
        {
            var data = DataGenerator.Generate().Take(100000).ToList();
            var data2 = DataGenerator.Generate().Take(100000).ToList();
            var differences = 10000;
            data.Modify(differences);
            var configuration = new KeyValueBloomFilterConfiguration();
            //create the first estimator
            var estimator = new BitMinwiseHashEstimator<TestEntity, long, sbyte>(
               configuration,
               2,
              20,
                data.LongCount());
            foreach(var element in data)
            estimator.Add(element);
            //create the second estimator
           var estimator2 = new BitMinwiseHashEstimator<TestEntity,long, sbyte>(configuration, 2, 20, data.LongCount());
             foreach(var element in data2)
                //just making sure we do not depend upon the order of adding things.
            estimator2.Add(element);
            var totalCount = data.LongCount() + data2.LongCount();
            //calculate the similarity between the two estimators.
            var differenceCount = totalCount - estimator.Similarity(estimator2) * totalCount;
            //within 95% or higher of difference count.
            Assert.IsTrue(differenceCount >= 0.95 * differences);
        }

        /// <summary>
        /// Compress an estimator.
        /// </summary>
        [TestMethod]
        public void BitMinwiseEstimatorCompressTest()
        {
            var data = DataGenerator.Generate().Take(100).ToArray();
            var configuration = new KeyValueBloomFilterConfiguration();
            var estimator = new BitMinwiseHashEstimator<TestEntity, long, sbyte>(configuration, 2, 20, 1000*data.LongCount());
            foreach (var element in data)
                estimator.Add(element);
            Assert.AreEqual(estimator.FullExtract().Capacity, 100170, "Unexpected capacity before compression.");
            var emptyEstimator = new BitMinwiseHashEstimator<TestEntity, long, sbyte>(configuration, 2, 20, 1000 * data.LongCount());
            Assert.IsTrue(estimator.Similarity(emptyEstimator) > 0.999D, "Unexpected similartiy before compression.");
            estimator.Compress(true);
            Assert.AreEqual(estimator.FullExtract().Capacity, 105, "Unexpected capacity after compression.");
            Assert.IsTrue(estimator.Similarity(emptyEstimator) > 0.999D, "Unexpected similartiy after compression.");
        }
    }
}
