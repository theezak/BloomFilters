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
    using BloomFilters.MathExt;    /// <summary>
                                   /// A full working test harness for creating an estimator, serializing the estimator and receiving the filter.
                                   /// </summary>
    internal class PrecalculatedActor
    {
        private readonly RuntimeTypeModel _protobufModel;
        private readonly IList<TestEntity> _dataSet;
        private readonly IHybridEstimatorFactory _hybridEstimatorFactory;
       private readonly IBloomFilterConfiguration<TestEntity, long, int,  short> _configuration;
        private readonly IInvertibleBloomFilterFactory _bloomFilterFactory;
        private readonly IHybridEstimator<TestEntity, int, short> _estimator;
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
            IBloomFilterConfiguration<TestEntity, long, int,  short> configuration)
        {
            _protobufModel = TypeModel.Create();
            _protobufModel.UseImplicitZeroDefaults = true;
            _dataSet = dataSet;
            _hybridEstimatorFactory = hybridEstimatorFactory;
            //TODO: hide this in a factory. Typically the estimator is heavily undersized when it comes to capacity.
            _estimator = new HybridEstimator<TestEntity, long,  short>(5000, 2, 14, _dataSet.Count, 7, configuration);
            foreach (var itm in _dataSet)
            {
                _estimator.Add(itm);
            }
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
            var fold = _configuration.FoldingStrategy.GetFoldFactors(_estimator.FullExtract().BlockSize,
                otherEstimator.BlockSize);
            //tricky: can't fold the estimator data. Well, you can, just not the bit min wise estimator.
            var estimator = _estimator.Fold((uint)fold.Item1, false);           
            return estimator.Extract().Decode(otherEstimator, _configuration);
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
                (IHybridEstimatorData<int, short>)
                    _protobufModel.Deserialize(estimatorStream, null, typeof(HybridEstimatorData<int, short>));
            //TODO: awkward just to get the capacity. Fix that.
            var estimatorData = _estimator.FullExtract();
            //TODO: using knowledge that both estimators were equally sized, thus other estimator size is a factor of the estimator capacity.
            //TODO; handle when that is not the case.
            var estimator = _estimator.Fold((uint)(estimatorData.BlockSize / otherEstimator.BlockSize), false);
            var estimate = estimator.Extract().Decode(otherEstimator, _configuration);
            if (estimate == null)
            {
                //additional communication step needed to create a new estimator.
                byte failedDecodeCount = 0;
                while (estimate == null && failedDecodeCount < 5)
                {
                    //TODO: not only capacity, but strata goes in to this as well.
                    //Strata requires truly a new estimator to be created.
                    //So in those cases ... ?
                    //Now:technically you could create a strata 9, because you will not pay the price for it unless you use it and on small set sizes
                    //there is little chance you'll use them.
                    var factors = MathExtensions.GetFactors(estimatorData.BlockSize);
                    var foldFactor = (uint)factors.OrderByDescending(f => f).Where(f => estimatorData.BlockSize / f > 80).Skip(failedDecodeCount).First();

                    estimator = _estimator.Fold(foldFactor, false);
                
                    using (var stream = new MemoryStream())
                    {
                        _protobufModel.Serialize(stream, estimator.Extract());
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
                var fullData = _estimator.FullExtract();
                var factors = MathExtensions.GetFactors(fullData.BlockSize);
                var foldFactor =(uint) factors.OrderByDescending(f => f).First(f =>  fullData.BlockSize / f > 80);
                var data = _estimator.Fold(foldFactor, false).Extract();
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
