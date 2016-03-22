namespace TBag.BloomFilters
{
    /// <summary>
    /// Interface for an invertible Bloom filter factory.
    /// </summary>
    public interface IInvertibleBloomFilterFactory
    {
        /// <summary>
        /// Create an invertible Bloom filter that will be utilized above its capacity.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="capacity">The capacity</param>
        /// <param name="errorRate">The optional error rate (between 0 and 1)</param>
        /// <returns></returns>
        /// <remarks>Estimators will utilize Bloom filters with a capacity set to the estimated number of differences, but then add the whole set. This results in much higher count values than a Bloom filter with a capacity equal to the set size would deal with.</remarks>
        IInvertibleBloomFilter<TEntity, TId, int> CreateHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, TId, int, int> bloomFilterConfiguration,
            long capacity,
            float? errorRate = null)
             where TId :struct;

        /// <summary>
        /// Create an invertible Bloom filter based upon the received Bloom filter data.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="capacity">The capacity</param>
        /// <param name="invertibleBloomFilterData">The received Bloom filter data to subtract.</param>
        /// <returns></returns>
        /// <remarks>Estimators will utilize Bloom filters with a capacity set to the estimated number of differences, but then add the whole set. This results in much higher count values than a Bloom filter with a capacity equal to the set size would deal with.</remarks>
        IInvertibleBloomFilter<TEntity, TId, int> CreateMatchingHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, TId, int, int> bloomFilterConfiguration,
            long capacity,
            IInvertibleBloomFilterData<TId, int, int> invertibleBloomFilterData)
            where TId : struct;

        /// <summary>
        /// Create a Bloom filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="capacity">The capacity</param>
        /// <param name="errorRate">The optional error rate (between 0 and 1)</param>
        /// <returns>The Bloom filter data</returns>
        IInvertibleBloomFilter<TEntity, TId, sbyte> Create<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, TId, int,  sbyte> bloomFilterConfiguration,
            long capacity,
            float? errorRate = null)
            where TId : struct;
    }
}