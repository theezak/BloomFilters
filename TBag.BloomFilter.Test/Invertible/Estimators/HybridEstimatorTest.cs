namespace TBag.BloomFilter.Test.Invertible.Estimators
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;
    using BloomFilters.Invertible.Estimators;
    using Infrastructure;    
    
    /// <summary>
    /// Summary description for hybridEstimatorTest
    /// </summary>
    [TestClass]
    public class HybridEstimatorTest
    {
        /// <summary>
        /// Fill two estimators and determine the number of differences.
        /// </summary>
        [TestMethod]
        public void HybridEstimatorBasicFillAndEstimate()
        {
            var data = DataGenerator.Generate().Take(10000).ToArray();
            var configuration = new KeyValueLargeBloomFilterConfiguration();
            var factory = new HybridEstimatorFactory();
            var estimator = factory.Create(configuration, data.Length);
            foreach (var element in data)
                estimator.Add(element);
            Assert.AreEqual(estimator.ItemCount, data.LongLength, "Estimator item count is wrong");
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
            Assert.AreEqual(estimator2.ItemCount, data.LongLength, "Second estimator item count is wrong");
            var differenceCount = estimator.Decode(estimator2);
            Assert.IsTrue(differenceCount >=  2*halfTheDiff, "Estimate below the difference count.");
            
        }

        /// <summary>
        /// Compress an estimator.
        /// </summary>
        [TestMethod]
        public void HybridEstimatorCompressTest()
        {
            var data = DataGenerator.Generate().Take(10000).ToArray();
            var configuration = new KeyValueLargeBloomFilterConfiguration();
            var factory = new HybridEstimatorFactory();
            var estimator = factory.Create(configuration, 2000*data.Length);
            Assert.AreEqual(estimator.BlockSize, 1170, "Unexpected block size before compression.");
            foreach (var element in data.Take(50))
                estimator.Add(element);
            var estimator2 = factory.Create(configuration, 2000*data.Length);
            foreach (var element in data.Skip(50).Take(50))
                estimator2.Add(element);
            var estimateBeforeCompression = estimator.Decode(estimator2);
            estimator.Compress(true);          
            Assert.AreEqual(estimator.BlockSize, 65, "Compression resulted in unexpected block size.");
            var estimateAfterCompression = estimator.Decode(estimator2);
            //note: rather tricky. Both estimators should have the same item count, otherwise compression does impact the result,
            //since one estimator no longer fits in the compressed one.
            Assert.AreEqual(estimateAfterCompression, estimateBeforeCompression, "Estimate changed due to compression.");
        }

        /// <summary>
        /// Test for removing items from the hybrid estimator.
        /// </summary>
        [TestMethod]
        public void HybridEstimatorRemoveTest()
        {
            //create data
            var data = DataGenerator.Generate().Take(10000).ToArray();
            //create the estimator
            var configuration = new KeyValueLargeBloomFilterConfiguration();
            var factory = new HybridEstimatorFactory();
            var estimator = factory.Create(configuration, data.Length);
            //add data to the estimator
            foreach (var element in data)
                estimator.Add(element);
            //create a second estimator
            var estimator2 = factory.Create(configuration, data.Length);
            foreach (var element in data)
                estimator2.Add(element);
            //estimate differences
            var estimateBeforeRemoval = estimator.Decode(estimator2);
            Assert.AreEqual(estimateBeforeRemoval, 0, "Unexpected number of differences before removing items.");
            //remove 100 items from the estimator
            foreach (var item in data.Take(100))
            {
                estimator.Remove(item);
            }
            //compare differences
            var estimateAfterRemoval = estimator.Decode(estimator2);
            Assert.IsTrue(estimateAfterRemoval > 100, "Removal from estimator resulted in not enough differences.");
        }

        /// <summary>
        /// Test quasi decoding. 
        /// </summary>
        /// <remarks>Note that this approach is rather error prone on estimators (due to their high false positive error rate on membership tests), but it can be an acceptable alternative when keeping an estimator maintained on each side is not possible.</remarks>
        [TestMethod]
        public void HybridEstimatorQuasiDecodeTest()
        {
            var data = DataGenerator.Generate().Take(100000).ToList();
            var configuration = new KeyValueLargeBloomFilterConfiguration();
            var factory = new HybridEstimatorFactory();
            var estimator = factory.Create(configuration, data.Count);
            foreach (var element in data)
                estimator.Add(element);
            data = DataGenerator.Generate().Skip(500).Take(8000).ToList();
            data.Modify(1000);
             var estimate = estimator.QuasiDecode(configuration, data);
            //actual difference is expected to be about 91500
            Assert.IsTrue(estimate>90500 && estimate <97000, "Unexpected estimate for difference." );
        }

        /// <summary>
        /// Test quasi decoding. 
        /// </summary>
        /// <remarks>Note that this approach is rather error prone on estimators (due to their high false positive error rate on membership tests), but it can be an acceptable alternative when keeping an estimator maintained on each side is not possible.</remarks>
        [TestMethod]
        public void HybridEstimatorQuasiRandomDecodeTest()
        {
            var data = DataGenerator.Generate().Take(900000).ToList();
            var configuration = new KeyValueLargeBloomFilterConfiguration();
            var factory = new HybridEstimatorFactory();
            var estimator = factory.Create(configuration, data.Count);
            foreach (var element in data)
                estimator.Add(element);
           // data = DataGenerator.Generate().Skip(500).Take(20).ToList();
            data.Modify(1000);
            var estimate = estimator.QuasiDecode(configuration, data);
            //actual difference is expected to be about 91500
        }
    }
}
