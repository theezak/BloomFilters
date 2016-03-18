namespace TBag.BloomFilter.Test
{
    using ProtoBuf.Meta;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BloomFilters;
    using BloomFilters.Estimators;

    /// <summary>
    /// A full working test harness for creating an estimator, serializing the estimator and receiving the filter.
    /// </summary>
    internal class Actor
    {
        private readonly RuntimeTypeModel _protobufModel;
        private readonly IList<TestEntity> _dataSet;
        private readonly IHybridEstimatorFactory _hybridEstimatorFactory;
        private readonly IBloomFilterConfiguration<TestEntity, long, int, int, int> _configuration;
        private readonly IInvertibleBloomFilterFactory _bloomFilterFactory;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataSet">The data set for this actor</param>
        /// <param name="hybridEstimatorFactory">Factory for creating estimators</param>
        /// <param name="bloomFilterFactory">Factory for creating Bloom filters</param>
        /// <param name="configuration">Bloom filter configuration to use</param>
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
        /// Given a serialized estimator (<paramref name="estimatorStream"/>), determine the size of the difference, create a Bloom filter for the difference and return that Bloom filter
        /// </summary>
        /// <param name="estimatorStream">The estimator</param>
        /// <returns></returns>
        public MemoryStream RequestFilter(MemoryStream estimatorStream)
        {
            var otherEstimator =
                (IHybridEstimatorData<int, int>)
                    _protobufModel.Deserialize(estimatorStream, null, typeof(HybridEstimatorData<int, int>));
            var estimator = _hybridEstimatorFactory.CreateMatchingEstimator(otherEstimator, _configuration,
                _dataSet.LongCount());
            foreach (var item in _dataSet)
            {
                estimator.Add(item);
            }
            var estimate = estimator.Extract().Decode(otherEstimator, _configuration);
            if (estimate==null)
            {
                //TODO: add communication step to create a new estimator.
                estimate = Math.Max(estimator.Extract().CountEstimate, otherEstimator.CountEstimate);
            }
             var filter = _bloomFilterFactory.CreateHighUtilizationFilter(_configuration, estimate.Value);
            foreach (var item in _dataSet)
            {
                filter.Add(item);
            }
            var result = new MemoryStream();
            _protobufModel.Serialize(result, filter.Extract());
            result.Position = 0;
            return result;
        }

        /// <summary>
        /// Given the actor, determine the difference.
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
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
                //send the estimator to the other actor and receive the filter from that actor.
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

}
