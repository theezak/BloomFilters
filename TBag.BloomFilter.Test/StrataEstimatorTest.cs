using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilters;
using TBag.HashAlgorithms;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using TBag.BloomFilters.Estimators;

namespace TBag.BloomFilter.Test
{
    [TestClass]
    public class StrataEstimatorTest
    {
      // [TestMethod]
        public void StrataEstimatorPerformanceMeasurement()
        {
            var configuration = new SingleBucketBloomFilterConfiguration {SplitByHash = true};
            var testSizes = new int[] { 1000, 10000, 100000,  500000 };
            var errorSizes = new int[] { 0, 1, 5, 10, 20, 50, 75, 100 };
            var capacities = new long[] { 15, 100, 1000 };
            foreach (var dataSize in testSizes)
            {
               
                    using (var writer = new StreamWriter(System.IO.File.Open($"multibucket-strataestimator-{dataSize}.csv", FileMode.Create)))
                    {
                        writer.WriteLine("duration,dataSize,capacity,modCount,estimatedModCount,modDiff");
                    foreach (var errorSize in errorSizes)
                    {
                        foreach (var capacity in capacities)
                    {
                      
                            var testData = DataGenerator.Generate().Take(dataSize).ToList();
                            var modCount = (int)((dataSize / 100.0D) * errorSize);
                            var testData2 = DataGenerator.Generate().Take(dataSize).ToList();
                            DataGenerator.Modify(testData2, modCount);
                            var startTime = DateTime.UtcNow;
                            var estimator1 = new StrataEstimator<TestEntity, long,sbyte>(capacity, configuration);
                            foreach (var item in testData)
                            {
                                estimator1.Add(item);
                            }
                            var estimator2 = new StrataEstimator<TestEntity, long,sbyte>(capacity, configuration);
                            foreach (var item in testData2)
                            {
                                estimator2.Add(item);
                            }
                            var measuredModCount = estimator1.Decode(estimator2);
                            var time = DateTime.UtcNow.Subtract(startTime);
                            writer.WriteLine($"{time.TotalMilliseconds},{dataSize},{capacity},{modCount},{measuredModCount},{(long)measuredModCount-modCount}");
                        }

                    }
                }
 
            }

        }
     
        [TestMethod]
        public void SimpleStrata()
        {
            var configuration = new SingleBucketBloomFilterConfiguration();
            configuration.SplitByHash = true;
            var testData = DataGenerator.Generate().Take(10000).ToList();
            IHashAlgorithm murmurHash = new Murmur3();
            var estimator1 = new StrataEstimator<TestEntity, long, sbyte>(80, configuration);
            foreach(var itm in testData)
            {
                estimator1.Add(itm);
            }
            foreach(var remove in testData.Take(10).ToArray())
            {
                testData.Remove(remove);
            }
            var estimator2 = new StrataEstimator<TestEntity, long, sbyte>(80, configuration);
            testData.Reverse();
            //just making sure we do not depend upon the order of adding things.
            foreach (var itm in testData) 
            {
                estimator2.Add(itm);
            }
            var estimate = estimator1.Decode(estimator2);
        }
    }
}
