namespace TBag.BloomFilter.Test
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;
    using TBag.BloomFilters;
    using System.IO;
    using TBag.BloomFilters.Estimators;

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

        /// <summary>
        /// Generate performance data for the hybrid estimator.
        /// </summary>
      [TestMethod]
        public void HybridEstimatorPerformanceMeasurement()
        {
            var configuration = new LargeBloomFilterConfiguration();
            var testSizes = new int[] { 1000, 10000, 100000, 500000 };
            var errorSizes = new int[] { 0, 1, 5, 10, 20, 50, 75, 100 };
            var capacities = new long[] { 15, 250, 2000 };
            var stratas = new byte[] { 3, 7, 13,  19, 25, 32 };
            foreach (var dataSize in testSizes)
            {
              
                    foreach (var errorSize in errorSizes)
                    {
                        using (
                            var writer =
                                new StreamWriter(System.IO.File.Open($"hybridestimator-{dataSize}-{errorSize}.csv",
                                    FileMode.Create)))
                        {
                            writer.WriteLine("duration,dataSize,strata,capacity,modCount,estimatedModCount,countDiff,countDiffSd");                          
                                foreach (var capacity in capacities)
                                {
                                    foreach (var strata in stratas)
                                    {
                                var timeSpanAggregate = new TimeSpan[50];
                                var countAggregate = new int[50];
                                var modCountResultAggregate = new int[50];

                                for (var run = 0; run < 50; run++)
                                {
                                    var testData = DataGenerator.Generate().Take(dataSize).ToList();
                                        var modCount = (int) ((dataSize/100.0D)*errorSize);
                                        var startTime = DateTime.UtcNow;
                                        var estimator1 = new HybridEstimator<TestEntity, long, int>(capacity, 2, 10,
                                            (uint) testData.Count, strata, configuration);
                                        foreach (var item in testData)
                                        {
                                            estimator1.Add(item);
                                        }
                                        testData.Modify(modCount);
                                        var estimator2 = new HybridEstimator<TestEntity, long, int>(capacity, 2, 10,
                                            (uint) testData.Count, strata, configuration);
                                        foreach (var item in testData)
                                        {
                                            estimator2.Add(item);
                                        }
                                        var measuredModCount = estimator1.Decode(estimator2);
                                    timeSpanAggregate[run] = DateTime.UtcNow.Subtract(startTime);
                                        countAggregate[run] = modCount;
                                        modCountResultAggregate[run] = (int)measuredModCount;
                                        
                                    }
                                        var timeAvg = new TimeSpan((long)timeSpanAggregate.Select(t=>t.Ticks).Average());
                                        var countAvg = (long)countAggregate.Average();
                                        var modCountResult = (long) modCountResultAggregate.Average();
                                        var differenceResult =
                                            modCountResultAggregate.Select((r,i) => r - countAggregate[i]).ToArray();
                                        var differenceSd = Math.Sqrt(differenceResult.Variance());                                      
                                writer.WriteLine($"{timeAvg.TotalMilliseconds},{dataSize},{strata},{capacity},{countAvg},{modCountResult},{(long)differenceResult.Average()},{differenceSd}");
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
            var configuration = new DefaultBloomFilterConfiguration();
            var estimator = new HybridEstimator<TestEntity, long, sbyte>(
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
            var estimator2 = new HybridEstimator<TestEntity, long, sbyte>(
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
