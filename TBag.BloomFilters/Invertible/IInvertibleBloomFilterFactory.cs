namespace TBag.BloomFilters.Invertible
{
    using Configurations;

    /// <summary>
    /// Interface for an invertible Bloom filter factory.
    /// </summary>
    public interface IInvertibleBloomFilterFactory
    {
        /// <summary>
        /// Create an invertible Bloom filter based upon the received Bloom filter data.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <typeparam name="TCount">THe type of the counter</typeparam>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="capacity">The capacity</param>
        /// <param name="invertibleBloomFilterData">The received Bloom filter data to subtract.</param>
        /// <returns></returns>
        /// <remarks>Estimators will utilize Bloom filters with a capacity set to the estimated number of differences, but then add the whole set. This results in much higher count values than a Bloom filter with a capacity equal to the set size would deal with.</remarks>
        IInvertibleBloomFilter<TEntity, TId, TCount> CreateMatchingHighUtilizationFilter<TEntity, TId, TCount>(
             IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> bloomFilterConfiguration,
             long capacity,
            IInvertibleBloomFilterData<TId, int, TCount> invertibleBloomFilterData)
             where TId : struct
             where TCount : struct;
 
        /// <summary>
        /// Create a Bloom filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <typeparam name="TCount">The counter type</typeparam>
        /// <typeparam name
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="capacity">The capacity</param>
        /// <param name="errorRate">The optional error rate (between 0 and 1)</param>
        /// <param name="reverse">Wen <c>true</c> a reverse IBF is created, else <c>false</c>.</param>
        /// <returns>The Bloom filter data</returns>
        IInvertibleBloomFilter<TEntity, TId, TCount> Create<TEntity, TId, TCount>(
               IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> bloomFilterConfiguration,
               long capacity,
               float? errorRate = null,
               bool reverse = false)
               where TId : struct
               where TCount : struct;
    }
}