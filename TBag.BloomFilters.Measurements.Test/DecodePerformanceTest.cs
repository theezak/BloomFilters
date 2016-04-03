using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;
using TBag.BloomFilters;
using System.Linq;
using System.IO;
using System.Threading;
using ProtoBuf.Meta;
using TBag.BloomFilters.Invertible;

namespace TBag.BloomFilters.Measurements.Test
{
    [TestClass]
    public class DecodePerformanceTest
    {
        private readonly RuntimeTypeModel _protobufTypeModel;
        public DecodePerformanceTest()
        {
            _protobufTypeModel = TypeModel.Create();
            _protobufTypeModel.UseImplicitZeroDefaults = true;
        }

        [TestMethod]
        public void RibfDecodePerSizePerformance()
        {
            var configuration = new KeyValueLargeBloomFilterConfiguration();

            var size = new[] { 1000, 10000, 100000 };
            var modPercentage = new[] { 0, 0.01D, 0.1D, 0.2D, 0.5D, 1.0D };
            foreach (var s in size)
            {
                using (
                           var writer =
                               new StreamWriter(File.Open($"ribfdecodespersize-{s}.csv",
                                   FileMode.Create)))
                {
                    writer.WriteLine("capacity,modCount,estimatedModCount,size,decodesPerSize,decodesPerSizeSd,decodeSuccessRate");

                    foreach (var mod in modPercentage)
                    {

                        foreach (var capacity in new[] { 10, 100, 500, 1000, 2000, 5000, 10000  })
                        {
                            var countSize= 0;
                            var decodesPerSize = new double[50];
                            var decodeResult = new int[50];
                            var modCountResultAggregate = new int[50];
                            for (var run = 0; run < 50; run++)
                            {
                                var dataSet1 = DataGenerator.Generate().Take(s).ToList();
                                var dataSet2 = DataGenerator.Generate().Take(s).ToList();
                                dataSet2.Modify((int) (s*mod));
                                var onlyInSet1 = dataSet1.Where(d => dataSet2.All(d2 => d2.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
                                var onlyInSet2 = dataSet2.Where(d => dataSet1.All(d1 => d1.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
                                var modified = dataSet1.Where(d => dataSet2.Any(d2 => d2.Id == d.Id && d2.Value != d.Value)).Select(d => d.Id).OrderBy(id => id).ToArray();

                                var bloomFilter1 = new InvertibleReverseBloomFilter<TestEntity, long, int>(configuration);
                                bloomFilter1.Initialize(capacity, 0.01F);

                                foreach (var item in dataSet1)
                                {
                                    bloomFilter1.Add(item);
                                }
                                var bloomFilter2 = new InvertibleReverseBloomFilter<TestEntity, long, int>(configuration);
                                bloomFilter2.Initialize(capacity, 0.01F);
                                foreach (var item in dataSet2)
                                {
                                    bloomFilter2.Add(item);
                                }
                                var s1 = new HashSet<long>();
                                var s2 = new HashSet<long>();
                                var s3 = new HashSet<long>();
                                decodeResult[run] = bloomFilter1.SubtractAndDecode(bloomFilter2, s1, s2, s3) ? 1 : 0;
                                var mods = s1.Union(s2).Union(s3).ToArray();                               
                                modCountResultAggregate[run] = mods.Count(v => onlyInSet1.Contains(v) ||
                               onlyInSet2.Contains(v) || modified.Contains(v));
                                decodesPerSize[run] = 1.0D * modCountResultAggregate[run] / bloomFilter1.Extract().Counts.Length;
                                countSize = bloomFilter1.Extract().Counts.Length;
                            }
                            writer
                                .WriteLine($"{capacity},{s * mod},{modCountResultAggregate.Average()},{countSize},{decodesPerSize.Average()},{Math.Sqrt(decodesPerSize.Variance())},{decodeResult.Average()}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Measure the subtract and decode step performance of an invertible Bloom filter. Key metric is the percentage of differences correctly identified.
        /// </summary>       
        [TestMethod]
        public void SplitRibfDecodePerformance()
        {
            var configuration = new KeyValueLargeBloomFilterConfiguration();

            var size = new[] { 1000, 10000, 100000 };
            var modPercentage = new[] { 0, 0.01D, 0.1D, 0.2D, 0.5D, 1.0D };
            foreach (var s in size)
            {
                using (
                           var writer =
                               new StreamWriter(File.Open($"splitribfdecode-{s}.csv",
                                   FileMode.Create)))
                {
                    writer.WriteLine("timeInMs,sizeInBytes,capacity,modCount,detectedModCount,countDiff,countDiffSd,decodeSuccessRate");

                    foreach (var mod in modPercentage)
                    {
                        foreach (var capacityPercentage in new[] { 0.5, 1, 2, 5, 10, 100 })
                        {
                            var sizeInBytes = new long[100];
                            var timeSpan = new long[50];
                            var countAggregate = new int[50];
                            var modCountResultAggregate = new int[50];
                              var decodeResult = new int[50];
                            for (var run = 0; run < 50; run++)
                            {                                
                                var dataSet1 = DataGenerator.Generate().Take(s).ToList();
                                var dataSet2 = DataGenerator.Generate().Take(s).ToList();
                                dataSet2.Modify((int)(s * mod));
                                var onlyInSet1 = dataSet1.Where(d => dataSet2.All(d2 => d2.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
                                var onlyInSet2 = dataSet2.Where(d => dataSet1.All(d1 => d1.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
                                var modified = dataSet1.Where(d => dataSet2.Any(d2 => d2.Id == d.Id && d2.Value != d.Value)).Select(d => d.Id).OrderBy(id => id).ToArray();
                                var idealCapacity = Math.Max(15, onlyInSet1.Count() + onlyInSet2.Count() + modified.Count());
                                var stopWatch = new Stopwatch();
                                stopWatch.Start();
                                var bloomFilter1 = new InvertibleReverseBloomFilter<TestEntity, long, int>(configuration);
                                bloomFilter1.Initialize((int)(idealCapacity * capacityPercentage), 0.01F);

                                foreach (var item in dataSet1)
                                {
                                    bloomFilter1.Add(item);
                                }
                                var bloomFilter2 = new InvertibleReverseBloomFilter<TestEntity, long, int>(configuration);
                                bloomFilter2.Initialize((int)(idealCapacity * capacityPercentage), 0.01F);
                                foreach (var item in dataSet2)
                                {
                                    bloomFilter2.Add(item);
                                }
                                var s1 = new HashSet<long>();
                                var s2 = new HashSet<long>();
                                var s3 = new HashSet<long>();
                                var success = bloomFilter1.SubtractAndDecode(bloomFilter2, s1, s2, s3);
                                stopWatch.Stop();
                                using (var stream = new MemoryStream())
                                {
                                    _protobufTypeModel.Serialize(stream, bloomFilter1.Extract());
                                    stream.Position = 0;
                                    sizeInBytes[run] = stream.Length;
                                }
                                using (var stream = new MemoryStream())
                                {
                                    _protobufTypeModel.Serialize(stream, bloomFilter2.Extract());
                                    stream.Position = 0;
                                    sizeInBytes[50+run] = stream.Length;
                                }
                                timeSpan[run] = stopWatch.ElapsedMilliseconds;
                                countAggregate[run] = onlyInSet1.Count() + onlyInSet2.Count() + modified.Count();
                                modCountResultAggregate[run] = s1.Union(s2).Union(s3).Count(v => onlyInSet1.Contains(v) ||
                                onlyInSet2.Contains(v) || modified.Contains(v));
                                 decodeResult[run] = success ? 1 : 0;
                            }
                            var countAvg = (long)countAggregate.Average();
                            var modCountResult = (long)modCountResultAggregate.Average();
                            var differenceResult =
                                modCountResultAggregate.Select((r, i) => r - countAggregate[i]).ToArray();
                            var differenceSd = Math.Sqrt(differenceResult.Variance());
                            writer
                                .WriteLine($"{timeSpan.Average()},{sizeInBytes.Average()},{Math.Max(15, capacityPercentage * (int)(s * mod))},{countAvg},{modCountResult},{(long)differenceResult.Average()},{differenceSd},{1.0D * decodeResult.Sum() / 50}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Measure the subtract and decode step performance of an invertible Bloom filter. Key metric is the percentage of differences correctly identified.
        /// </summary>       
        [TestMethod]
        public void RibfDecodePerformance()
        {
            var configuration = new KeyValueLargeBloomFilterConfiguration();
            var size = new[] { 1000, 10000, 100000 };
            var modPercentage = new[] { 0, 0.01D, 0.1D, 0.2D, 0.5D, 1.0D };
            foreach (var s in size)
            {
                using (
                           var writer =
                               new StreamWriter(File.Open($"ribfdecode-{s}.csv",
                                   FileMode.Create)))
                {
                    writer.WriteLine("timeInMs,sizeInBytes,capacity,modCount,detectedModCount,countDiff,countDiffSd,decodeSuccessRate,listADiff,listBDiff,listCDiff");
                    foreach (var mod in modPercentage)
                    {
                        foreach (var capacityPercentage in new[] { 0.5, 1, 2, 5, 10, 100 })
                        {
                            var sizeInBytes = new long[100];
                            var timeSpan = new long[50];
                            var countAggregate = new int[50];
                            var modCountResultAggregate = new int[50];
                            var listADiff = new int[50];
                            var listBDiff = new int[50];
                            var listCDiff = new int[50];
                            var decodeResult = new int[50];
                            for (var run = 0; run < 50; run++)
                            {
                                var dataSet1 = DataGenerator.Generate().Take(s).ToList();
                                var dataSet2 = DataGenerator.Generate().Take(s).ToList();
                                dataSet2.Modify((int)(s * mod));
                                var onlyInSet1 = dataSet1.Where(d => dataSet2.All(d2 => d2.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
                                var onlyInSet2 = dataSet2.Where(d => dataSet1.All(d1 => d1.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
                                var modified = dataSet1.Where(d => dataSet2.Any(d2 => d2.Id == d.Id && d2.Value != d.Value)).Select(d => d.Id).OrderBy(id => id).ToArray();
                                var idealCapacity = Math.Max(15, onlyInSet1.Count() + onlyInSet2.Count() + modified.Count());
                                var stopWatch = new Stopwatch();
                                stopWatch.Start();
                                var bloomFilter1 = new InvertibleReverseBloomFilter<TestEntity, long, int>( configuration);
                                bloomFilter1.Initialize((int)(idealCapacity * capacityPercentage), 0.01F);

                                foreach (var item in dataSet1)
                                {
                                    bloomFilter1.Add(item);
                                }
                                var bloomFilter2 = new InvertibleReverseBloomFilter<TestEntity, long, int>(configuration);
                                bloomFilter2.Initialize((int)(idealCapacity * capacityPercentage), 0.01F);
                                foreach (var item in dataSet2)
                                {
                                    bloomFilter2.Add(item);
                                }
                                var s1 = new HashSet<long>();
                                var s2 = new HashSet<long>();
                                var s3 = new HashSet<long>();
                                var success = bloomFilter1.SubtractAndDecode(bloomFilter2, s1, s2, s3);
                                stopWatch.Stop();
                                using (var stream = new MemoryStream())
                                {
                                    _protobufTypeModel.Serialize(stream, bloomFilter1.Extract());
                                    stream.Position = 0;
                                    sizeInBytes[run] = stream.Length;
                                }
                                using (var stream = new MemoryStream())
                                {
                                    _protobufTypeModel.Serialize(stream, bloomFilter2.Extract());
                                    stream.Position = 0;
                                    sizeInBytes[50 + run] = stream.Length;
                                }
                                timeSpan[run] = stopWatch.ElapsedMilliseconds;                                
                                countAggregate[run] = onlyInSet1.Count() + onlyInSet2.Count() + modified.Count();
                                modCountResultAggregate[run] = s1.Union(s2).Union(s3).Count(v => onlyInSet1.Contains(v) ||
                                onlyInSet2.Contains(v) || modified.Contains(v));
                                listADiff[run] = s1.Count() - onlyInSet1.Count();
                                listBDiff[run] = s2.Count() - onlyInSet2.Count();
                                listCDiff[run] = s3.Count() - modified.Count();
                                decodeResult[run] = success ? 1 : 0;
                            }
                            var countAvg = (long)countAggregate.Average();
                            var modCountResult = (long)modCountResultAggregate.Average();
                            var differenceResult =
                                modCountResultAggregate.Select((r, i) => r - countAggregate[i]).ToArray();
                            var differenceSd = Math.Sqrt(differenceResult.Variance());
                            writer
                                .WriteLine($"{timeSpan.Average()},{sizeInBytes.Average()},{Math.Max(15, capacityPercentage * (int)(s * mod))},{countAvg},{modCountResult},{(long)differenceResult.Average()},{differenceSd},{1.0D*decodeResult.Sum()/50},{listADiff.Average()},{listBDiff.Average()},{listCDiff.Average()}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Measure the subtract and decode step performance of an invertible Bloom filter. Key metric is the percentage of differences correctly identified.
        /// </summary>       
        [TestMethod]
        public void HybridIbfDecodePerformance()
        {
            var configuration = new KeyValueLargeBloomFilterConfiguration();

            var size = new[] { 1000, 10000, 100000 };
            var modPercentage = new[] { 0, 0.01D, 0.1D, 0.2D, 0.5D, 1.0D };
            foreach (var s in size)
            {
                using (
                           var writer =
                               new StreamWriter(File.Open($"hibfdecode-{s}.csv",
                                   FileMode.Create)))
                {
                    writer.WriteLine("timeInMs,sizeInBytes,capacity,modCount,detectedModCount,countDiff,countDiffSd,decodeSuccessRate,listADiff,listBDiff,listCDiff");

                    foreach (var mod in modPercentage)
                    {

                        foreach (var capacityPercentage in new[] { 0.5, 1, 2, 5, 10, 100 })
                        {
                            var sizeInBytes = new long[100];
                            var timeSpan = new long[50];
                            var countAggregate = new int[50];
                            var modCountResultAggregate = new int[50];
                            var listADiff = new int[50];
                            var listBDiff = new int[50];
                            var listCDiff = new int[50];
                            var decodeResult = new int[50];
                            for (var run = 0; run < 50; run++)
                            {
                                var dataSet1 = DataGenerator.Generate().Take(s).ToList();
                                var dataSet2 = DataGenerator.Generate().Take(s).ToList();
                                dataSet2.Modify((int)(s * mod));
                                var onlyInSet1 = dataSet1.Where(d => dataSet2.All(d2 => d2.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
                                var onlyInSet2 = dataSet2.Where(d => dataSet1.All(d1 => d1.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
                                var modified = dataSet1.Where(d => dataSet2.Any(d2 => d2.Id == d.Id && d2.Value != d.Value)).Select(d => d.Id).OrderBy(id => id).ToArray();
                                var idealCapacity = Math.Max(15, onlyInSet1.Count() + onlyInSet2.Count() + modified.Count());
                                var stopWatch = new Stopwatch();
                                stopWatch.Start();
                                var bloomFilter1 = new InvertibleHybridBloomFilter<TestEntity, long, int>(configuration);
                                bloomFilter1.Initialize((int)(idealCapacity * capacityPercentage), 0.01F);

                                foreach (var item in dataSet1)
                                {
                                    bloomFilter1.Add(item);
                                }
                                var bloomFilter2 = new InvertibleHybridBloomFilter<TestEntity, long, int>(configuration);
                                bloomFilter2.Initialize((int)(idealCapacity * capacityPercentage), 0.01F);
                                foreach (var item in dataSet2)
                                {
                                    bloomFilter2.Add(item);
                                }
                                var s1 = new HashSet<long>();
                                var s2 = new HashSet<long>();
                                var s3 = new HashSet<long>();
                                var success = bloomFilter1.SubtractAndDecode(bloomFilter2, s1, s2, s3);
                                stopWatch.Stop();
                                using (var stream = new MemoryStream())
                                {
                                    _protobufTypeModel.Serialize(stream, bloomFilter1.Extract());
                                    stream.Position = 0;
                                    sizeInBytes[run] = stream.Length;
                                }
                                using (var stream = new MemoryStream())
                                {
                                    _protobufTypeModel.Serialize(stream, bloomFilter2.Extract());
                                    stream.Position = 0;
                                    sizeInBytes[50 + run] = stream.Length;
                                }
                                timeSpan[run] = stopWatch.ElapsedMilliseconds;
                                countAggregate[run] = onlyInSet1.Count() + onlyInSet2.Count() + modified.Count();
                                modCountResultAggregate[run] = s1.Union(s2).Union(s3).Count(v => onlyInSet1.Contains(v) ||
                                onlyInSet2.Contains(v) || modified.Contains(v));
                                listADiff[run] = s1.Count() - onlyInSet1.Count();
                                listBDiff[run] = s2.Count() - onlyInSet2.Count();
                                listCDiff[run] = s3.Count() - modified.Count();
                                decodeResult[run] = success ? 1 : 0;
                            }
                            var countAvg = (long)countAggregate.Average();
                            var modCountResult = (long)modCountResultAggregate.Average();
                            var differenceResult =
                                modCountResultAggregate.Select((r, i) => r - countAggregate[i]).ToArray();
                            var differenceSd = Math.Sqrt(differenceResult.Variance());
                            writer
                                .WriteLine($"{timeSpan.Average()},{sizeInBytes.Average()},{Math.Max(15, capacityPercentage * (int)(s * mod))},{countAvg},{modCountResult},{(long)differenceResult.Average()},{differenceSd},{1.0D * decodeResult.Sum() / 50},{listADiff.Average()},{listBDiff.Average()},{listCDiff.Average()}");
                        }
                    }
                }
            }
        }
    }
}
