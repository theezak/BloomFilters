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
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <typeparam name="TCount">The type of occurence count.</typeparam>
        /// <param name="configuration">Bloom filter configuration</param>
        /// <param name="setSize">Number of elements in the set that is added.</param>
        /// <param name="failedDecodeCount">Number of times decoding has failed based upon the provided estimator.</param>
        /// <returns></returns>
        public IHybridEstimator<TEntity, TId, TCount> Create<TEntity, TId, TCount>(
            IBloomFilterConfiguration<TEntity, int, TId, long, TCount> configuration,
            long setSize,
            byte failedDecodeCount = 0)
            where TCount : struct
        {
            byte strata = 7;
            var capacity = 80L;
            if (setSize < 10000L && failedDecodeCount >0)
            {
                capacity = capacity * failedDecodeCount * 10;
                if (failedDecodeCount >= 4 && failedDecodeCount <= 6)
                {
                    //higher strata is typically not preferred with lower capacities, but try it at this point.
                    strata = 13;
                }
            }
            if (setSize >= 10000L)
            {
                capacity *= 2L;
            }
            if (setSize > 500000L)
            {
                capacity = 1000;
                if (failedDecodeCount > 0)
                {
                    strata = 13;
                }
            }        
            if (setSize >= 1000000L)
            {
                strata = 13;
                if (failedDecodeCount > 0)
                {
                    strata = 19;
                }
            }
            var result = new HybridEstimator<TEntity, TId, TCount>(capacity, 2, 10, (ulong) setSize, strata,
                configuration)
            {
                DecodeCountFactor = Math.Pow(2, failedDecodeCount)
            };
            return result;
        }

        public IHybridEstimator<TEntity, TId, TCount> CreateMatchingEstimator<TEntity, TId, TCount>(IHybridEstimatorData<TId, TCount> data,
            IBloomFilterConfiguration<TEntity, int, TId, long, TCount> configuration,
            long setSize) where TCount : struct
        {
           var estimator = new HybridEstimator<TEntity, TId, TCount>(
               data.Capacity, 
               data.BitMinwiseEstimator.BitSize, 
               data.BitMinwiseEstimator.HashCount, 
               (ulong)setSize, 
               data.StrataCount, 
               configuration);
            estimator.DecodeCountFactor = data.StrataEstimator.DecodeCountFactor;
            return estimator;
        }
    }
}
