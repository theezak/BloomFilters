using System;

namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Encapsulates emperical data for creating hybrid estimators.
    /// </summary>
    public class HybridEstimatorFactory : IHybridEstimatorFactory
    {
        /// <summary>
        /// Create a hybrid estimator
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TCount">The type of occurence count.</typeparam>
        /// <param name="configuration">Bloom filter configuration</param>
        /// <param name="setSize">Number of elements in the set that is added.</param>
        /// <param name="failedDecodeCount">Number of times decoding has failed based upon the provided estimator.</param>
        /// <returns></returns>
        public IHybridEstimator<TEntity, int, TCount> Create<TEntity, TId, TCount>(
            IBloomFilterConfiguration<TEntity,  TId, int, int, TCount> configuration,
            long setSize,
            byte failedDecodeCount = 0)
            where TCount : struct
            where TId : struct
        {
            byte strata = 7;
            var capacity = 80L;
            if (failedDecodeCount > 2)
            {
                capacity = capacity * failedDecodeCount;
            }
            if (setSize < 10000L &&
                failedDecodeCount >= 4 &&
                failedDecodeCount <= 6)
            {
                strata = 3;
            }
            if (setSize >= 10000L)
            {
                capacity *= 2L;
                if (capacity > 250 &&
                    failedDecodeCount > 2)
                {
                    strata = 13;
                }
            }
            if (setSize > 500000L &&
                failedDecodeCount > 1)
            {
                strata = 13;
            }
            var result = new HybridEstimator<TEntity, TId, TCount>(
                capacity, 
                2, 
                10, 
                setSize, 
                strata,
                configuration)
            {
                DecodeCountFactor = Math.Pow(2, failedDecodeCount)
            };
            return result;
        }

        public IHybridEstimator<TEntity, int, TCount> CreateMatchingEstimator<TEntity, TId, TCount>(
            IHybridEstimatorData<int, TCount> data,
            IBloomFilterConfiguration<TEntity, TId, int, int, TCount> configuration,
            long setSize) 
            where TCount : struct
            where TId : struct
           {
           var estimator = new HybridEstimator<TEntity, TId, TCount>(
               data.Capacity, 
               data.BitMinwiseEstimator.BitSize, 
               data.BitMinwiseEstimator.HashCount, 
               setSize, 
               data.StrataCount, 
               configuration);
            estimator.DecodeCountFactor = data.StrataEstimator.DecodeCountFactor;
            return estimator;
        }
    }
}
