using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf.Meta;
using TBag.BloomFilters;
using TBag.BloomFilters.Estimators;

namespace TBag.BloomFilter.Test
{
    [TestClass]
    public class RoundTripTest
    {
        private class Actor
        {
            private readonly RuntimeTypeModel _protobufModel;
            private readonly IList<TestEntity> _dataSet;
            private readonly IHybridEstimatorFactory _hybridEstimatorFactory;
            private readonly IBloomFilterConfiguration<TestEntity, long, int, int, int> _configuration;
            private readonly IInvertibleBloomFilterFactory _bloomFilterFactory;
            public Actor(IList<TestEntity> dataSet,
                IHybridEstimatorFactory hybridEstimatorFactory,
                IInvertibleBloomFilterFactory bloomFilterFactory,
                IBloomFilterConfiguration<TestEntity, long, int, int, int> configuration)
            {
                _protobufModel = TypeModel.Create();
                _protobufModel.UseImplicitZeroDefaults = true;
                _dataSet = dataSet;
                _hybridEstimatorFactory = hybridEstimatorFactory;
                _bloomFilterFactory = bloomFilterFactory;
                _configuration = configuration;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="estimatorStream">The estimator</param>
            /// <returns></returns>
            public MemoryStream RequestFilter(MemoryStream estimatorStream)
            {
                var otherEstimator =
                    (IHybridEstimatorData<int, int>)
                        _protobufModel.Deserialize(estimatorStream, null, typeof (HybridEstimatorData<int, int>));
                var estimator = _hybridEstimatorFactory.CreateMatchingEstimator(otherEstimator, _configuration,
                    _dataSet.LongCount());
                foreach (var item in _dataSet)
                {
                    estimator.Add(item);
                }
                  //note: even though the estimator might be right on, with larger differences, a much larger filter is needed.
               //smaller (but not insignificant) differences with high set sizes seems to be the challenge.
               //TODO: filter will miss changed entities first unless sized larger. This is just a hack, gather some performance data.
                var filter = _bloomFilterFactory.CreateHighUtilizationFilter(_configuration, estimator.Extract(), otherEstimator);
                foreach (var item in _dataSet)
                {
                    filter.Add(item);
                }
                var result = new MemoryStream();
                _protobufModel.Serialize(result, filter.Extract());
                result.Position = 0;
                return result;
            }

            public Tuple<HashSet<long>, HashSet<long>, HashSet<long>> GetDifference(Actor actor)
            {
                var estimator = _hybridEstimatorFactory.Create(_configuration, _dataSet.LongCount());                
                foreach (var item in _dataSet)
                {
                    estimator.Add(item);
                }
                using (var estimatorStream = new MemoryStream())
                {
                    _protobufModel.Serialize(estimatorStream, estimator.Extract());
                    estimatorStream.Position = 0;
                    var otherFilterStream = actor.RequestFilter(estimatorStream);
                    var otherFilter = (IInvertibleBloomFilterData<long, int, int>)
                        _protobufModel.Deserialize(otherFilterStream, null, typeof(InvertibleBloomFilterData<long, int, int>));
                    otherFilterStream.Dispose();
                    var filter = _bloomFilterFactory.CreateMatchingHighUtilizationFilter(_configuration,
                        _dataSet.LongCount(), otherFilter);
                    foreach (var item in _dataSet)
                    {
                        filter.Add(item);
                    }
                    var onlyInThisSet = new HashSet<long>();
                    var onlyInOtherSet = new HashSet<long>();
                    var modified = new HashSet<long>();
                    var succes = filter.SubtractAndDecode(otherFilter, onlyInThisSet, onlyInOtherSet, modified);
                    //note: even when not successfully decoded for sure, the sets will contain info.
                    return new Tuple<HashSet<long>, HashSet<long>, HashSet<long>>(onlyInThisSet, onlyInOtherSet, modified);
                }
            }
        }        

        /// <summary>
        /// Test a full round trip of 1) sending an estimator 2) receiving an estimator and determining the number of differences 3) sending a filter and 4) receiving a filter and decoding.
        /// </summary>
        [TestMethod]
        public void TestRoundTrip()
        {
            var configuration = new LargeBloomFilterConfiguration();
            IHybridEstimatorFactory estimatorFactory = new HybridEstimatorFactory();
            IInvertibleBloomFilterFactory bloomFilterFactory = new InvertibleBloomFilterFactory();
            var dataSet1 = DataGenerator.Generate().Take(10000).ToList();
            var actor1 = new Actor(
                dataSet1,
                estimatorFactory,
                bloomFilterFactory,
                configuration);
            var dataSet2 = DataGenerator.Generate().Take(10000).ToList();
            dataSet2.Modify(1000);
            var actor2 = new Actor(
                dataSet2,
                estimatorFactory,
                bloomFilterFactory,
                configuration);
            var result = actor1.GetDifference(actor2);
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
