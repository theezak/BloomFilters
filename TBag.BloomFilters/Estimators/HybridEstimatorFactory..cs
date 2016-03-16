namespace TBag.BloomFilters.Estimators
{
    using System;

    /// <summary>
    /// Encapsulates emperical data for creating hybrid estimators.
    /// </summary>
    public class HybridEstimatorFactory : IHybridEstimatorFactory
    {
        /// <summary>
        /// Create a hybrid estimator
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
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
            byte hashFunctionCount = 4;
            float errorRate = 0.001F;
            if (failedDecodeCount > 2)
            {
                capacity = capacity * failedDecodeCount;
                errorRate = 0.0001F;
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
                errorRate,
                configuration,
                hashFunctionCount)
            {
                DecodeCountFactor = Math.Pow(2, failedDecodeCount)
            };
            return result;
        }

        /// <summary>
        /// Create an estimator that matches the given <paramref name="data"/> estimator.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the identifier</typeparam>
        /// <typeparam name="TCount">The type of the occurence count</typeparam>
        /// <param name="data">The estimator data to match</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="setSize">The (estimated) size of the set to add to the estimator.</param>
        /// <returns>An estimator</returns>
        public IHybridEstimator<TEntity, int, TCount> CreateMatchingEstimator<TEntity, TId, TCount>(
            IHybridEstimatorData<int, TCount> data,
            IBloomFilterConfiguration<TEntity, TId, int, int, TCount> configuration,
            long setSize)
            where TCount : struct
            where TId : struct
        {
            return new HybridEstimator<TEntity, TId, TCount>(
                data.Capacity,
                data.BitMinwiseEstimator.BitSize,
                data.BitMinwiseEstimator.HashCount,
                setSize,
                data.StrataCount,
                data.StrataEstimator.ErrorRate,
                configuration,
                data.StrataEstimator.HashFunctionCount)
            {
                DecodeCountFactor = data.StrataEstimator.DecodeCountFactor
            };
        }
    }
}
