using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using TBag.BloomFilters;
using System.IO;
using TBag.BloomFilters.Estimators;

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

       // [TestMethod]
        public void HybridEstimatorPerformanceMeasurement()
        {
            var configuration = new SingleBucketBloomFilterConfiguration
            {
                 SplitByHash =  true
            };
            var testSizes = new int[] { 1000, 10000, 100000, 500000 };
            var errorSizes = new int[] { 0, 1, 5, 10, 20, 50, 75, 100 };
            var capacities = new long[] { 15, 100, 1000 };
            var stratas = new byte[] { 3, 7, 13,  19, 25, 32 };
            foreach (var dataSize in testSizes)
            {
              
                    foreach (var errorSize in errorSizes)
                    {
                        using (var writer = new StreamWriter(System.IO.File.Open($"multibucket-hybridestimator-{dataSize}-{errorSize}.csv", FileMode.Create)))
                        {
                            writer.WriteLine("duration,dataSize,strata,capacity,modCount,estimatedModCount,countDiff");

                        foreach (var capacity in capacities)
                        {
                            foreach (var strata in stratas)
                    {
                        
                                var testData = DataGenerator.Generate().Take(dataSize).ToList();
                                var modCount = (int)((dataSize / 100.0D) * errorSize);
                                var testData2 = DataGenerator.Generate().Take(dataSize).ToList();
                                DataGenerator.Modify(testData2, modCount);
                                var startTime = DateTime.UtcNow;
                                var estimator1 = new HybridEstimator<TestEntity, long,byte>(capacity, 2, 30, (uint)testData.Count, strata, configuration);
                                foreach (var item in testData)
                                {
                                    estimator1.Add(item);
                                }
                                var estimator2 = new HybridEstimator<TestEntity, long,byte>(capacity, 2, 30, (uint)testData2.Count, strata, configuration);
                                foreach (var item in testData2)
                                {
                                    estimator2.Add(item);
                                }
                                var measuredModCount = estimator1.Decode(estimator2);
                                var time = DateTime.UtcNow.Subtract(startTime);
                                writer.WriteLine($"{time.TotalMilliseconds},{dataSize},{strata},{capacity},{modCount},{measuredModCount},{ (long)measuredModCount-modCount}");
                            }
                        }
                    }
                }

            }
        

        }


        [TestMethod]
        public void BasicFillAndEstimate()
        {
            var data = DataGenerator.Generate().Take(200000).ToArray();
            var configuration = new SingleBucketBloomFilterConfiguration();
            var estimator = new HybridEstimator<TestEntity, long, byte>(
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
            var estimator2 = new HybridEstimator<TestEntity, long, byte>(
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
