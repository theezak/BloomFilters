namespace TBag.BloomFilter.Test
{
    using ProtoBuf.Meta;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BloomFilters;
    using BloomFilters.Estimators;
    using BloomFilters.Configurations;
    using BloomFilters.Invertible;
    using BloomFilters.Invertible.Configurations;
    using BloomFilters.Invertible.Estimators;/// <summary>
                                             /// A full working test harness for creating an estimator, serializing the estimator and receiving the filter.
                                             /// </summary>
    internal class Actor
    {
        private readonly RuntimeTypeModel _protobufModel;
        private readonly IList<TestEntity> _dataSet;
        private readonly IHybridEstimatorFactory _hybridEstimatorFactory;
       private readonly IInvertibleBloomFilterConfiguration<TestEntity, long, int,  short> _configuration;
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
            IInvertibleBloomFilterConfiguration<TestEntity, long, int,  short> configuration)
        {
            _protobufModel = TypeModel.Create();
            _protobufModel.UseImplicitZeroDefaults = true;
            _dataSet = dataSet;
            _hybridEstimatorFactory = hybridEstimatorFactory;
            _bloomFilterFactory = bloomFilterFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// Given an estimator, get an estimate.
        /// </summary>
        /// <param name="estimatorStream"></param>
        /// <returns></returns>
        /// <remarks>Needed only when the first estimate fails. An alternative is now calculating the filter here and sending it to the other actor (depends upon which actor wants to know the difference).</remarks>
        public long? GetEstimate(MemoryStream estimatorStream)
        {
            var otherEstimator =
               (HybridEstimatorData<int, short>)
                   _protobufModel.Deserialize(estimatorStream, null, typeof(HybridEstimatorData<int, short>));
            var estimator = _hybridEstimatorFactory.CreateMatchingEstimator(otherEstimator, _configuration,
                _dataSet.LongCount());
            foreach (var item in _dataSet)
            {
                estimator.Add(item);
            }
            return estimator.Decode(otherEstimator);
        }

        /// <summary>
        /// Given a serialized estimator (<paramref name="estimatorStream"/>), determine the size of the difference, create a Bloom filter for the difference and return that Bloom filter
        /// </summary>
        /// <param name="estimatorStream">The estimator</param>
        /// <returns></returns>
        public MemoryStream RequestFilter(MemoryStream estimatorStream, Actor otherActor)
        {
            var otherEstimator =
                (IHybridEstimatorData<int, short>)
                    _protobufModel.Deserialize(estimatorStream, null, typeof(HybridEstimatorData<int, short>));
            var estimator = _hybridEstimatorFactory.CreateMatchingEstimator(otherEstimator, _configuration,
                _dataSet.LongCount());
            foreach (var item in _dataSet)
            {
                estimator.Add(item);
            }
            var estimate = estimator.Decode(otherEstimator);
            if (estimate == null)
            {
                //additional communication step needed to create a new estimator.
                byte failedDecodeCount = 0;
                while (estimate == null && failedDecodeCount < 5)
                {
                    estimator = _hybridEstimatorFactory.Create(_configuration, _dataSet.Count(), ++failedDecodeCount);
                    foreach (var item in _dataSet)
                    {
                        estimator.Add(item);
                    }
                    using (var stream = new MemoryStream())
                    {
                        _protobufModel.Serialize(stream, estimator.Extract());
                        stream.Position = 0;
                        estimate = otherActor.GetEstimate(stream);
                    }
                }
                if (estimate == null)
                {
                    throw new NullReferenceException("Did not negotiate a good estimate");
                }
            }
             var filter = _bloomFilterFactory.Create(_configuration, estimate.Value, 0.001F, true);
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
                var data = estimator.Extract();
                _protobufModel.Serialize(estimatorStream, data);
                estimatorStream.Position = 0;
                //send the estimator to the other actor and receive the filter from that actor.
                var otherFilterStream = actor.RequestFilter(estimatorStream, this);
                var otherFilter = (IInvertibleBloomFilterData<long, int,short>)
                    _protobufModel.Deserialize(otherFilterStream, null, _configuration.DataFactory.GetDataType<long,int,short>());
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
                var succes = filter.SubtractAndDecode(onlyInThisSet, onlyInOtherSet, modified, otherFilter);
                //note: even when not successfully decoded for sure, the sets will contain info.
                return new Tuple<HashSet<long>, HashSet<long>, HashSet<long>>(onlyInThisSet, onlyInOtherSet, modified);
            }
        }
    }

}
