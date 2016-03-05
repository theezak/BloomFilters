using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TBag.BloomFilters;
using TBag.HashAlgorithms;
using System.Collections.Generic;
using System.Linq;

namespace TBag.BloomFilter.Test
{
    [TestClass]
    public class StrataEstimatorTest
    {
     
        [TestMethod]
        public void SimpleStrata()
        {
            var configuration = new SingleBucketBloomFilterConfiguration();
            configuration.SplitByHash = true;
            var testData = DataGenerator.Generate().Take(10000).ToList();
            IHashAlgorithm murmurHash = new Murmur3();
            var estimator1 = new StrataEstimator<TestEntity, long>(80, configuration);
            foreach(var itm in testData)
            {
                estimator1.Add(itm);
            }
            foreach(var remove in testData.Take(10).ToArray())
            {
                testData.Remove(remove);
            }
            var estimator2 = new StrataEstimator<TestEntity, long>(80, configuration);
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
