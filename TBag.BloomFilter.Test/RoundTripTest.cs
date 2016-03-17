namespace TBag.BloomFilter.Test
{
     using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
   using TBag.BloomFilters;
    using TBag.BloomFilters.Estimators;

    [TestClass]
    public class RoundTripTest
    {
        /// <summary>
        /// Test a full round trip of 1) sending an estimator 2) receiving an estimator and determining the number of differences 3) sending a filter and 4) receiving a filter and decoding.
        /// </summary>
        [TestMethod]
        public void TestRoundTrip()
        {
            var configuration = new LargeBloomFilterConfiguration();
            IHybridEstimatorFactory estimatorFactory = new HybridEstimatorFactory();
            IInvertibleBloomFilterFactory bloomFilterFactory = new InvertibleBloomFilterFactory();
            var dataSet1 = DataGenerator.Generate().Take(100000).ToList();
            //create the actors.
            var actor1 = new Actor(
                dataSet1,
                estimatorFactory,
                bloomFilterFactory,
                configuration);
            var dataSet2 = DataGenerator.Generate().Take(100000).ToList();
            dataSet2.Modify(50000);
            var actor2 = new Actor(
                dataSet2,
                estimatorFactory,
                bloomFilterFactory,
                configuration);
            //get the result
            var result = actor1.GetDifference(actor2);
            //analyze the result.
            var onlyInSet1 = dataSet1.Where(d => dataSet2.All(d2 => d2.Id != d.Id)).Select(d=>d.Id).OrderBy(id=>id).ToArray();
            var onlyInSet2 = dataSet2.Where(d => dataSet1.All(d1 => d1.Id != d.Id)).Select(d => d.Id).OrderBy(id => id).ToArray();
            var modified = dataSet1.Where(d => dataSet2.Any(d2 => d2.Id == d.Id && d2.Value != d.Value)).Select(d => d.Id).OrderBy(id => id).ToArray();
            var allFound = new HashSet<long>();
            foreach (var itm in result.Item1) allFound.Add(itm);
            foreach (var itm in result.Item2) allFound.Add(itm);
            foreach (var itm in result.Item3) allFound.Add(itm);
            var falsePositives =
                allFound.Where(itm => !onlyInSet1.Contains(itm) && !onlyInSet2.Contains(itm) && !modified.Contains(itm))
                    .ToArray();        
            var missed =
                onlyInSet1.Where(itm => !allFound.Contains(itm))
                    .Union(onlyInSet2.Where(itm => !allFound.Contains(itm)))
                    .Union(modified.Where(itm => !allFound.Contains(itm)))
                    .Distinct()
                    .ToArray();
        }
    }
}
