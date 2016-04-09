namespace TBag.BloomFilter.Test.Invertible.Estimators
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class StrataEstimatorTest
    {
      // [TestMethod]
        public void StrataEstimatorPerformanceMeasurement()
        {
            //TODO: need a strata estimator factory OR expose convert of config (so: strate estimator factory)

            //var configuration = new LargeBloomFilterConfiguration();
            //var testSizes = new int[] { 1000, 10000, 100000,  500000 };
            //var errorSizes = new int[] { 0, 1, 5, 10, 20, 50, 75, 100 };
            //var capacities = new long[] { 15, 100, 1000 };
            //foreach (var dataSize in testSizes)
            //{

            //        using (var writer = new StreamWriter(System.IO.File.Open($"multibucket-strataestimator-{dataSize}.csv", FileMode.Create)))
            //        {
            //            writer.WriteLine("duration,dataSize,capacity,modCount,estimatedModCount,modDiff");
            //        foreach (var errorSize in errorSizes)
            //        {
            //            foreach (var capacity in capacities)
            //        {

            //                var testData = DataGenerator.Generate().Take(dataSize).ToList();
            //                var modCount = (int)((dataSize / 100.0D) * errorSize);                            
            //                var startTime = DateTime.UtcNow;
            //                var estimator1 = new StrataEstimator<TestEntity, int>(capacity, configuration);
            //                foreach (var item in testData)
            //                {
            //                    estimator1.Add(item);
            //                }
            //                testData.Modify(modCount);
            //                var estimator2 = new StrataEstimator<TestEntity, int>(capacity, configuration);
            //                foreach (var item in testData)
            //                {
            //                    estimator2.Add(item);
            //                }
            //                var measuredModCount = estimator1.Decode(estimator2);
            //                var time = DateTime.UtcNow.Subtract(startTime);
            //                writer.WriteLine($"{time.TotalMilliseconds},{dataSize},{capacity},{modCount},{measuredModCount},{(long)measuredModCount-modCount}");
            //            }

            //        }
            //    }

            //}

        }

        [TestMethod]
        public void SimpleStrata()
        {
            //TODO: need a strata estimator factory OR expose convert of config (so: strate estimator factory)
            //var configuration = new DefaultBloomFilterConfiguration();
            //var testData = DataGenerator.Generate().Take(10000).ToList();
            //IHashAlgorithm murmurHash = new Murmur3();
            //var estimator1 = new StrataEstimator<TestEntity, sbyte>(80, configuration);
            //foreach(var itm in testData)
            //{
            //    estimator1.Add(itm);
            //}
            //foreach(var remove in testData.Take(10).ToArray())
            //{
            //    testData.Remove(remove);
            //}
            //var estimator2 = new StrataEstimator<TestEntity, sbyte>(80, configuration);
            //testData.Reverse();
            ////just making sure we do not depend upon the order of adding things.
            //foreach (var itm in testData) 
            //{
            //    estimator2.Add(itm);
            //}
            //var estimate = estimator1.Decode(estimator2);
        }
    }
}
