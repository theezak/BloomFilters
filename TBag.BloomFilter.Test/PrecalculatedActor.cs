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
    using BloomFilters.Invertible.Configurations;    /// <summary>
                                                     /// A full working test harness for creating an estimator, serializing the estimator and receiving the filter.
                                                     /// </summary>
    internal class PrecalculatedActor
    {
        private readonly RuntimeTypeModel _protobufModel;
        private readonly IList<TestEntity> _dataSet;
        private readonly IHybridEstimatorFactory _hybridEstimatorFactory;
       private readonly IInvertibleBloomFilterConfiguration<TestEntity, long, int,  sbyte> _configuration;
        private readonly IInvertibleBloomFilterFactory _bloomFilterFactory;
        private readonly HybridEstimator<TestEntity, long, sbyte> _estimator;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataSet">The data set for this actor</param>
        /// <param name="hybridEstimatorFactory">Factory for creating estimators</param>
        /// <param name="bloomFilterFactory">Factory for creating Bloom filters</param>
        /// <param name="configuration">Bloom filter configuration to use</param>
        public PrecalculatedActor(IList<TestEntity> dataSet,
            IHybridEstimatorFactory hybridEstimatorFactory,
            IInvertibleBloomFilterFactory bloomFilterFactory,
            IInvertibleBloomFilterConfiguration<TestEntity, long, int,  sbyte> configuration)           {
            _protobufModel = TypeModel.Create();
            _protobufModel.UseImplicitZeroDefaults = true;
            _dataSet = dataSet;
            _hybridEstimatorFactory = hybridEstimatorFactory;
            _bloomFilterFactory = bloomFilterFactory;
            _configuration = configuration;
            //terribly over size the estimator.
            _estimator = _hybridEstimatorFactory.Create(_configuration, 100000);
            foreach (var itm in _dataSet)
            {
                _estimator.Add(itm);
            }
            _estimator.Remove(_dataSet[0]);
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
               (HybridEstimatorData<int, sbyte>)
                   _protobufModel.Deserialize(estimatorStream, null, typeof(HybridEstimatorData<int, sbyte>));
            return _estimator.Decode(otherEstimator);
        }

        /// <summary>
        /// Given a serialized estimator (<paramref name="estimatorStream"/>), determine the size of the difference, create a Bloom filter for the difference and return that Bloom filter
        /// </summary>
        /// <param name="estimatorStream">The estimator</param>
        /// <param name="otherActor"></param>
        /// <returns></returns>
        public MemoryStream RequestFilter(MemoryStream estimatorStream, PrecalculatedActor otherActor)
        {
            var otherEstimator =
                (IHybridEstimatorData<int, sbyte>)
                    _protobufModel.Deserialize(estimatorStream, null, typeof(HybridEstimatorData<int, sbyte>));
            var estimate = _estimator.Decode(otherEstimator);
            if (estimate == null)
            {
                //additional communication step needed to create a new estimator.
                byte failedDecodeCount = 0;
                while (estimate == null && failedDecodeCount < 5)
                {
                    var estimator = _hybridEstimatorFactory.Extract(_configuration, _estimator, failedDecodeCount);
                    using (var stream = new MemoryStream())
                    {
                        _protobufModel.Serialize(stream, estimator);
                        stream.Position = 0;
                        estimate = otherActor.GetEstimate(stream);
                    }
                    failedDecodeCount++;
                }
                if (estimate == null)
                {
                    throw new NullReferenceException("Did not negotiate a good estimate");
                }
            }
            //TODO: strategy to also fold the filter beyond compress size to the error size.
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
        public Tuple<HashSet<long>, HashSet<long>, HashSet<long>> GetDifference(PrecalculatedActor actor)
        {           
            using (var estimatorStream = new MemoryStream())
            {
                //TODO: hide in a strategy for compressing the estimator (when more failures, less compresed)

                var data = _hybridEstimatorFactory.Extract(_configuration, _estimator);
                _protobufModel.Serialize(estimatorStream, data);
                estimatorStream.Position = 0;
                //send the estimator to the other actor and receive the filter from that actor.
                var otherFilterStream = actor.RequestFilter(estimatorStream, this);
                var otherFilter = (IInvertibleBloomFilterData<long, int,sbyte>)
                    _protobufModel.Deserialize(otherFilterStream, null, _configuration.DataFactory.GetDataType<long,int,sbyte>());
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
