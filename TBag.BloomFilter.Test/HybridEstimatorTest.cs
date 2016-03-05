using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using TBag.BloomFilters;

namespace TBag.BloomFilter.Test
{
    /// <summary>
    /// Summary description for hybridEstimatorTest
    /// </summary>
    [TestClass]
    public class HybridEstimatorTest
    {
        public HybridEstimatorTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void BasicFillAndEstimate()
        {
            var data = DataGenerator.Generate().Take(200000).ToArray();
            var configuration = new SingleBucketBloomFilterConfiguration();
            var estimator = new HybridEstimator<TestEntity, long>(
                220,
               /* quick initial testing shows:
                220 can handle a 100% error rate on 200000 elements
                160 can handle a 100% error rate on 100000
                80 can handle 10% error rate on 100000
                15 can handle 0.25% error rate on 100000 
                But the whole point is to explore this. The idea is: if this estimate results in an actual bloom filter that doesn't decode, increase this number on the estimator, estimate again and decode again.*/
               1,
                50,
                (uint)data.Length,
            7,
               configuration);
            foreach (var element in data)
                estimator.Add(element);
            var estimator2 = new HybridEstimator<TestEntity, long>(
                220,
               1,
                50,
                (uint)data.Length,
              7,
               configuration);
            foreach (var elt in data.Take(100000))
            {
                elt.Id += 1000000;
            }
            foreach (var elt in data.Skip(100000).Take(100000))
            {
                elt.Value += 10;
            }
            foreach (var element in data.Reverse())
                //just making sure we do not depend upon the order of adding things.
                estimator2.Add(element);
            var differenceCount = estimator.Decode(estimator2);
        }
    }
}
