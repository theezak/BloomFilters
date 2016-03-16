namespace TBag.BloomFilters
{
    using Estimators;

    /// <summary>
    /// Place holder for a factory to create Bloom filters based upon strata estimators.
    /// </summary>
    public class InvertibleBloomFilterFactory : IInvertibleBloomFilterFactory
    {
        /// <summary>
        /// Create an invertible Bloom filter for high utilization (many more items added than it was sized for).
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="capacity">The capacity</param>
        /// <param name="errorRate">The desired error rate (between 0 and 1)</param>
        /// <returns></returns>
        public IInvertibleBloomFilter<TEntity, TId, int> CreateHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity,TId,int,int, int> bloomFilterConfiguration,
            long capacity,
            float? errorRate = null)
            where TId : struct
        {
            if (errorRate.HasValue)
            {
                return new InvertibleReverseBloomFilter<TEntity, TId, int>(capacity, errorRate.Value, bloomFilterConfiguration);
            }
            return new InvertibleReverseBloomFilter<TEntity, TId, int>(capacity, bloomFilterConfiguration);
        }

        /// <summary>
        /// Create an invertible Bloom filter that is sized for the set size determined by the estimators <paramref name="estimator"/> and <paramref name="otherEstimator"/>.
        /// </summary>
        // <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="estimator">An estimator</param>
        /// <param name="otherEstimator">An estimator</param>
        /// <param name="errorRate">The desired error rate for the Bloom flter (between 0 and 1)</param>
        /// <param name="destructive">When <c>true</c> the estimator <paramref name="estimator"/> will be destroyed by this operation, else <c>false</c>.</param>
        /// <returns></returns>
        public IInvertibleBloomFilter<TEntity, TId, int> CreateHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, TId, int, int, int> bloomFilterConfiguration,
            IHybridEstimatorData<int, int> estimator,
            IHybridEstimatorData<int, int> otherEstimator,
            float? errorRate = null,
            bool destructive = false)
            where TId : struct
        {
            var estimate = estimator.Decode(otherEstimator, bloomFilterConfiguration, destructive);
            return CreateHighUtilizationFilter(
               bloomFilterConfiguration,
               (long)estimate,
               errorRate ?? 0.001F);
        }

        /// <summary>
        /// Create an invertible Bloom filter that is compatible with the given bloom filter data.
        /// </summary>
        // <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="capacity">The capacity for the filter</param>
        /// <param name="invertibleBloomFilterData">The data to match with this filter.</param>
        /// <returns></returns>
        /// <remarks>For the scenario where you need to match a received filter with the set you own, so you can find the differences.</remarks>
        public IInvertibleBloomFilter<TEntity, TId, int> CreateMatchingHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, TId, int, int, int> bloomFilterConfiguration,
            long capacity,
           IInvertibleBloomFilterData<TId, int, int> invertibleBloomFilterData)
            where TId : struct
        {
            var blockSize = invertibleBloomFilterData.BlockSize;          
            return new InvertibleReverseBloomFilter<TEntity, TId, int>(
                capacity, 
                blockSize, 
                invertibleBloomFilterData.HashFunctionCount, 
                bloomFilterConfiguration);
        }

        /// <summary>
        /// Create an invertible Bloom filter
        /// </summary>
        // <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="capacity">The capacity</param>
        /// <param name="errorRate">The desired error rate (between 0 and 1)</param>
        /// <returns></returns>
        /// <remarks>Assumption is that the utilization will be in line with the capacity, thus keeping individual counts low.</remarks>
        public IInvertibleBloomFilter<TEntity, TId, sbyte> Create<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, TId, int,int, sbyte> bloomFilterConfiguration,
            long capacity,
            float? errorRate = null)
            where TId : struct
        {
            return errorRate.HasValue ? 
                new InvertibleReverseBloomFilter<TEntity, TId, sbyte>(capacity, errorRate.Value, bloomFilterConfiguration) : 
                new InvertibleReverseBloomFilter<TEntity, TId, sbyte>(capacity, bloomFilterConfiguration);
        }
    }
}
