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
            var data = DataGenerator.Generate().Take(100000).ToArray();
            var configuration = new SingleBucketBloomFilterConfiguration();
            var estimator = new HybridEstimator<TestEntity, long>(
                80,
                2,
                50,
                (uint)data.Length,
                7,
               configuration);
            foreach (var element in data)
                estimator.Add(element);
            var estimator2 = new HybridEstimator<TestEntity, long>(
                80,
                2,
                50,
                (uint)data.Length,
                7,
               configuration);
            foreach (var elt in data.Take(20000))
            {
                elt.Id += 200000;
            }
            foreach (var elt in data.Skip(20000).Take(20000))
            {
                elt.Value += 10;
            }
            foreach (var element in data)
                estimator2.Add(element);
            var differenceCount = estimator.Decode(estimator2);
        }
    }
}
