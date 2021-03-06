﻿namespace TBag.BloomFilter.Test.Infrastructure
{
    using ProtoBuf.Meta;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using BloomFilters.Invertible;
    using BloomFilters.Invertible.Configurations;
    using BloomFilters.Invertible.Estimators;

    /// <summary>
    /// A full working test harness for creating an estimator, serializing the estimator and receiving the filter.
    /// </summary>
    internal class PrecalculatedActor<TCount>
        where TCount : struct
    {
        private readonly RuntimeTypeModel _protobufModel;
        private readonly IHybridEstimatorFactory _hybridEstimatorFactory;
        private readonly IInvertibleBloomFilterConfiguration<TestEntity, long, int, TCount> _configuration;
        private readonly HybridEstimator<TestEntity, long, TCount> _estimator;
        private readonly IInvertibleBloomFilter<TestEntity, long, TCount> _filter;

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
            IInvertibleBloomFilterConfiguration<TestEntity, long, int, TCount> configuration)
        {
            _protobufModel = TypeModel.Create();
            _protobufModel.UseImplicitZeroDefaults = true;
             _hybridEstimatorFactory = hybridEstimatorFactory;
            _configuration = configuration;
            //terribly over size the estimator.
            _estimator = _hybridEstimatorFactory.Create(_configuration, 100000);
            foreach (var itm in dataSet)
            {
                _estimator.Add(itm);
            }
            //sized to number of differences it can handle, not to the size of the data.
            _filter = bloomFilterFactory.Create(_configuration, 5000, 0.001F, true);
            foreach (var item in dataSet)
            {
                _filter.Add(item);
            }
        }

        /// <summary>
        /// Given an estimator, get an estimate.
        /// </summary>
        /// <param name="estimatorStream"></param>
        /// <returns></returns>
        /// <remarks>Needed only when the first estimate fails. An alternative is now calculating the filter here and sending it to the other actor (depends upon which actor wants to know the difference).</remarks>
        public long? GetEstimate(MemoryStream estimatorStream)
        {
            Console.WriteLine($"Estimator size: {estimatorStream.Length} ");
            var otherEstimator =
                (HybridEstimatorData<int, TCount>)
                    _protobufModel.Deserialize(estimatorStream, null, typeof (HybridEstimatorData<int, TCount>));
            return _estimator.Decode(otherEstimator);
        }

        /// <summary>
        /// Given a serialized estimator (<paramref name="estimatorStream"/>), determine the size of the difference, create a Bloom filter for the difference and return that Bloom filter
        /// </summary>
        /// <param name="estimatorStream">The estimator</param>
        /// <param name="otherActor"></param>
        /// <returns></returns>
        public MemoryStream RequestFilter(MemoryStream estimatorStream, PrecalculatedActor<TCount> otherActor)
        {
            Console.WriteLine($"Estimator size: {estimatorStream.Length} ");
            var otherEstimator =
                (IHybridEstimatorData<int, TCount>)
                    _protobufModel.Deserialize(estimatorStream, null, typeof (HybridEstimatorData<int, TCount>));
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
            var result = new MemoryStream();
            _protobufModel.Serialize(result, _configuration.DataFactory.Extract(_configuration, _filter, estimate.Value));
            result.Position = 0;
            Console.WriteLine($"Filter size: {result.Length} ");
            return result;
        }

        /// <summary>
        /// Given the actor, determine the difference.
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
        public Tuple<HashSet<long>, HashSet<long>, HashSet<long>> GetDifference(PrecalculatedActor<TCount> actor)
        {
            using (var estimatorStream = new MemoryStream())
            {
                var data = _hybridEstimatorFactory.Extract(_configuration, _estimator);
                _protobufModel.Serialize(estimatorStream, data);
                estimatorStream.Position = 0;
                //send the estimator to the other actor and receive the filter from that actor.
                var otherFilterStream = actor.RequestFilter(estimatorStream, this);
                var otherFilter = (IInvertibleBloomFilterData<long, int, TCount>)
                    _protobufModel.Deserialize(otherFilterStream, null,
                        _configuration.DataFactory.GetDataType<long, int, TCount>());
                otherFilterStream.Dispose();
                var onlyInThisSet = new HashSet<long>();
                var onlyInOtherSet = new HashSet<long>();
                var modified = new HashSet<long>();
                var succes = _filter.SubtractAndDecode(onlyInThisSet, onlyInOtherSet, modified, otherFilter);
                //note: even when not successfully decoded for sure, the sets will contain info.
                return new Tuple<HashSet<long>, HashSet<long>, HashSet<long>>(onlyInThisSet, onlyInOtherSet, modified);
            }
        }
    }
}