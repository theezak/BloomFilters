namespace TBag.BloomFilter.Test.Invertible
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using BloomFilters.Invertible;
    using BloomFilters.Invertible.Estimators;
    using Infrastructure;
    using System.Diagnostics;
    using System;
    [TestClass]
    public class RoundTripTest
    {
        /// <summary>
        /// Test a full round trip of 1) sending an estimator 2) receiving an estimator and determining the number of differences 3) sending a filter and 4) receiving a filter and decoding.
        /// </summary>
        [TestMethod]
        public void TestRoundTrip()
        {
            //choosing a counter type that is too small will result in many overflows (which manifests itself in horribly slow performance).
            //Keep the count type large enough, so extreme folds do not cause overflows. Benefit of folds outweighs benefit of small count types.
            var configuration = new KeyValueLargeBloomFilterConfiguration();
            IHybridEstimatorFactory estimatorFactory = new HybridEstimatorFactory();
            IInvertibleBloomFilterFactory bloomFilterFactory = new InvertibleBloomFilterFactory();
            //create the first actor
            var dataSet1 = DataGenerator.Generate().Take(15000).ToList();
            var actor1 = new Actor<short>(
                dataSet1,
                estimatorFactory,
                bloomFilterFactory,
                configuration);
            //create the second actor
            var dataSet2 = DataGenerator.Generate().Take(17000).ToList();
            dataSet2.Modify(1000);
            var actor2 = new Actor<short>(
                dataSet2,
                estimatorFactory,
                bloomFilterFactory,
                configuration);
            //have actor 1 determine the difference with actor 2.
            var timer = new Stopwatch();
            timer.Start();
            var result = actor1.GetDifference(actor2);
            timer.Stop();
            Console.WriteLine($"Time: {timer.ElapsedMilliseconds} ms");
            //analyze results
            var allFound = new HashSet<long>(result.Item1.Union(result.Item2).Union(result.Item3));
            Assert.IsTrue(allFound.Count() > 3000, "Less than the expected number of diffferences found.");
            //analyze the result.
            var onlyInSet1 =
                dataSet1.Where(d => dataSet2.All(d2 => d2.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
            var onlyInSet2 =
                dataSet2.Where(d => dataSet1.All(d1 => d1.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
            var modified =
                dataSet1.Where(d => dataSet2.Any(d2 => d2.Id == d.Id && d2.Value != d.Value))
                    .Select(d => d.Id)
                    .OrderBy(id => id)
                    .ToArray();
            var falsePositives =
                allFound.Where(itm => !onlyInSet1.Contains(itm) && !onlyInSet2.Contains(itm) && !modified.Contains(itm))
                    .ToArray();
            Assert.IsTrue(falsePositives.Count() < 50, "Too many false positives found");
            var falseNegatives =
                onlyInSet1.Where(itm => !allFound.Contains(itm))
                    .Union(onlyInSet2.Where(itm => !allFound.Contains(itm)))
                    .Union(modified.Where(itm => !allFound.Contains(itm)))
                    .ToArray();
            Assert.IsTrue(falseNegatives.Count() < 25, "Too many false negatives found");
        }
    }
}
