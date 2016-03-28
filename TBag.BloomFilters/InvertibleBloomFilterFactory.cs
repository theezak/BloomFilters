namespace TBag.BloomFilters
{
    using Configurations;

    /// <summary>
    /// Place holder for a factory to create Bloom filters based upon strata estimators.
    /// </summary>
    public class InvertibleBloomFilterFactory : IInvertibleBloomFilterFactory
    {


        /// <summary>
        /// Create an invertible Bloom filter that is compatible with the given bloom filter data.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the counter</typeparam>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="capacity">The capacity for the filter</param>
        /// <param name="invertibleBloomFilterData">The data to match with this filter.</param>
        /// <returns>The created Bloom filter</returns>
        /// <remarks>For the scenario where you need to match a received filter with the set you own, so you can find the differences.</remarks>
        public IInvertibleBloomFilter<TEntity, TId, TCount> CreateMatchingHighUtilizationFilter<TEntity, TId, TCount>(
            IBloomFilterConfiguration<TEntity, TId, int, TCount> bloomFilterConfiguration,
            long capacity,
           IInvertibleBloomFilterData<TId, int, TCount> invertibleBloomFilterData)
            where TId : struct
            where TCount : struct
        {
            var ibf = invertibleBloomFilterData.IsReverse 
                ? new InvertibleReverseBloomFilter<TEntity, TId, TCount>(bloomFilterConfiguration)
                : new InvertibleBloomFilter<TEntity, TId, TCount>(bloomFilterConfiguration);
            ibf.Initialize(capacity, invertibleBloomFilterData.BlockSize, invertibleBloomFilterData.HashFunctionCount);
            return ibf;
        }

        /// <summary>
        /// Create an invertible Bloom filter
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">Type of the counter.</typeparam>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="capacity">The capacity</param>
        /// <param name="errorRate">The desired error rate (between 0 and 1)</param>
        /// <param name="reverse">When <c>true</c> a reverse IBF is created, else <c>false</c></param>
        /// <returns>The created Bloom filter</returns>
        /// <remarks>Assumption is that the utilization will be in line with the capacity, thus keeping individual counts low.</remarks>
        public IInvertibleBloomFilter<TEntity, TId, TCount> Create<TEntity, TId, TCount>(
            IBloomFilterConfiguration<TEntity, TId, int, TCount> bloomFilterConfiguration,
            long capacity,
            float? errorRate = null,
            bool reverse = false)
            where TId : struct
            where TCount : struct
        {
            if (capacity <= 0)
            {
                capacity = 1;
            }
            var ibf = reverse 
                ? new InvertibleReverseBloomFilter<TEntity, TId, TCount>(bloomFilterConfiguration)
                : new InvertibleBloomFilter<TEntity, TId, TCount>(bloomFilterConfiguration);
            if (errorRate.HasValue)
                ibf.Initialize(capacity, errorRate.Value);
            else
                ibf.Initialize(capacity);
            return ibf;
        }
    }
}
