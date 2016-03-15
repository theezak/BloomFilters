using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using TBag.BloomFilters;
using System.Linq;
using System.IO;

namespace TBag.BloomFilter.Test
{
    [TestClass]
    public class DecodePerformanceTest
    {
        /// <summary>
        /// Measure the subtract and decode step performance of an invertible Bloom filter. Key metric is the percentage of differences correctly identified.
        /// </summary>
        [TestMethod]
        public void DecodePerformance()
        {
            var configuration = new LargeBloomFilterConfiguration();

            var size = new int[] { 1000, 10000, 100000 };
            var modPercentage = new double[] { 0.01D, 0.1D, 0.2D, 0.5D, 1.0D };
            foreach (var s in size)
            {
                using (
                           var writer =
                               new StreamWriter(System.IO.File.Open($"bloomfilterdecode-{s}.csv",
                                   FileMode.Create)))
                {
                    writer.WriteLine("capacity,modCount,estimatedModCount,countDiff,countDiffSd");

                    foreach (var mod in modPercentage)
                    {

                        foreach (var capacityPercentage in new int[] { 1, 2, 5, 10, 100 })
                        {
                            var countAggregate = new int[50];
                            var modCountResultAggregate = new int[50];

                            for (var run = 0; run < 50; run++)
                            {
                                var dataSet1 = DataGenerator.Generate().Take(s).ToList();
                                var dataSet2 = DataGenerator.Generate().Take(s).ToList();
                                dataSet2.Modify((int)(s * mod));
                                var onlyInSet1 = dataSet1.Where(d => dataSet2.All(d2 => d2.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
                                var onlyInSet2 = dataSet2.Where(d => dataSet1.All(d1 => d1.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
                                var modified = dataSet1.Where(d => dataSet2.Any(d2 => d2.Id == d.Id && d2.Value != d.Value)).Select(d => d.Id).OrderBy(id => id).ToArray();
                                var idealCapacity = Math.Max(15, onlyInSet1.Count() + onlyInSet2.Count() + modified.Count());
                                var bloomFilter1 = new InvertibleReverseBloomFilter<TestEntity, long, int>(idealCapacity * capacityPercentage, 0.001F, configuration);
                                foreach (var item in dataSet1)
                                {
                                    bloomFilter1.Add(item);
                                }
                                var bloomFilter2 = new InvertibleReverseBloomFilter<TestEntity, long, int>(idealCapacity * capacityPercentage, 0.001F, configuration);
                                foreach (var item in dataSet2)
                                {
                                    bloomFilter2.Add(item);
                                }
                                var s1 = new HashSet<long>();
                                var s2 = new HashSet<long>();
                                var s3 = new HashSet<long>();
                                bloomFilter1.SubtractAndDecode(bloomFilter2, s1, s2, s3);
                                countAggregate[run] = onlyInSet1.Count() + onlyInSet2.Count() + modified.Count();
                                modCountResultAggregate[run] = s1.Union(s2).Union(s3).Where(v => onlyInSet1.Contains(v) ||
                                onlyInSet2.Contains(v) || modified.Contains(v)).Count();
                            }
                            var countAvg = (long)countAggregate.Average();
                            var modCountResult = (long)modCountResultAggregate.Average();
                            var differenceResult =
                                modCountResultAggregate.Select((r, i) => r - countAggregate[i]).ToArray();
                            var differenceSd = Math.Sqrt(differenceResult.Variance());
                            writer.WriteLine($"{Math.Max(15, capacityPercentage * (int)(s * mod))},{countAvg},{modCountResult},{(long)differenceResult.Average()},{differenceSd}");
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void DecodeTest()
        {
            var configuration = new LargeBloomFilterConfiguration();


            var dataSet1 = DataGenerator.Generate().Take(10).ToList();
            var dataSet2 = DataGenerator.Generate().Take(10).ToList();
            dataSet2[0].Value += "Test";
            var onlyInSet1 = dataSet1.Where(d => dataSet2.All(d2 => d2.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
            var onlyInSet2 = dataSet2.Where(d => dataSet1.All(d1 => d1.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
            var modified = dataSet1.Where(d => dataSet2.Any(d2 => d2.Id == d.Id && d2.Value != d.Value)).Select(d => d.Id).OrderBy(id => id).ToArray();
            var bloomFilter1 = new InvertibleReverseBloomFilter<TestEntity, long, int>(30, 0.001F, configuration);
            foreach (var item in dataSet1)
            {
                bloomFilter1.Add(item);
            }
            var bloomFilter2 = new InvertibleReverseBloomFilter<TestEntity, long, int>(30, 0.001F, configuration);
            foreach (var item in dataSet2)
            {
                bloomFilter2.Add(item);
            }
            var s1 = new HashSet<long>();
            var s2 = new HashSet<long>();
            var s3 = new HashSet<long>();
            var decodeResult = bloomFilter1.SubtractAndDecode(bloomFilter2, s1, s2, s3);
        }
    }
}
