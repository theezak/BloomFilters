namespace TBag.BloomFilters
{
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
        /// <returns>The created Bloom filter.</returns>
        public IInvertibleBloomFilter<TEntity, TId, int> CreateHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity,TId,int, int> bloomFilterConfiguration,
            long capacity,
            float? errorRate = null)
            where TId : struct
        {
            if (capacity <= 0)
            {
                capacity = 1;
            }
            var ibf =  new InvertibleReverseSplitBloomFilter<TEntity, TId, int>(bloomFilterConfiguration);
           if (errorRate.HasValue)
            {
                ibf.Initialize(capacity, errorRate.Value);
            }
           else
            {
                ibf.Initialize(capacity);
            }
            return ibf;
        }

        /// <summary>
        /// Create an invertible Bloom filter that is compatible with the given bloom filter data.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="capacity">The capacity for the filter</param>
        /// <param name="invertibleBloomFilterData">The data to match with this filter.</param>
        /// <returns>The created Bloom filter</returns>
        /// <remarks>For the scenario where you need to match a received filter with the set you own, so you can find the differences.</remarks>
        public IInvertibleBloomFilter<TEntity, TId, int> CreateMatchingHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, TId, int, int> bloomFilterConfiguration,
            long capacity,
           IInvertibleBloomFilterData<TId, int, int> invertibleBloomFilterData)
            where TId : struct
        {
            var ibf = invertibleBloomFilterData.IsReverse 
                ? (invertibleBloomFilterData.IdSums == null 
                ? (IInvertibleBloomFilter<TEntity,TId,int>)new InvertibleReverseSplitBloomFilter<TEntity, TId, int>(bloomFilterConfiguration, invertibleBloomFilterData.SubFilterCount)
                :new InvertibleReverseBloomFilter<TEntity, TId, int>(bloomFilterConfiguration))
                : new InvertibleBloomFilter<TEntity, TId, int>(bloomFilterConfiguration);
            ibf.Initialize(capacity, invertibleBloomFilterData.GetFilterBlockSize(), invertibleBloomFilterData.HashFunctionCount);
            return ibf;
        }

        /// <summary>
        /// Create an invertible Bloom filter
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="capacity">The capacity</param>
        /// <param name="errorRate">The desired error rate (between 0 and 1)</param>
        /// <returns>The created Bloom filter</returns>
        /// <remarks>Assumption is that the utilization will be in line with the capacity, thus keeping individual counts low.</remarks>
        public IInvertibleBloomFilter<TEntity, TId, sbyte> Create<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, TId, int, sbyte> bloomFilterConfiguration,
            long capacity,
            float? errorRate = null)
            where TId : struct
        {
            if (capacity <= 0)
            {
                capacity = 1;
            }
            var ibf = new InvertibleReverseSplitBloomFilter<TEntity, TId, sbyte>(bloomFilterConfiguration);
            if (errorRate.HasValue)
                ibf.Initialize(capacity, errorRate.Value);
            else
                ibf.Initialize(capacity);
            return ibf;
        }
    }
}
