namespace TBag.BloomFilters
{
    using TBag.BloomFilters.Estimators;

    /// <summary>
    /// Interface for an invertible Bloom filter factory.
    /// </summary>
    public interface IInvertibleBloomFilterFactory
    {
        /// <summary>
        /// Create an invertible Bloom filter that will be utilized above its capacity.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="bloomFilterConfiguration"></param>
        /// <param name="capacity"></param>
        /// <param name="errorRate"></param>
        /// <returns></returns>
        /// <remarks>Estimators will utilize Bloom filters with a capacity set to the estimated number of differences, but then add the whole set. This results in much higher count values than a Bloom filter with a capacity equal to the set size would deal with.</remarks>
        IInvertibleBloomFilter<TEntity, TId, int> CreateHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, TId,int,int, int> bloomFilterConfiguration,
            long capacity,
            float? errorRate = null)
             where TId :struct;

        /// <summary>
        /// Create an invertible Bloom filter based upon the estimators for two different sets.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="bloomFilterConfiguration"></param>
        /// <param name="estimator"></param>
        /// <param name="otherEstimator"></param>
        /// <param name="errorRate"></param>
        /// <param name="destructive"></param>
        /// <returns></returns>
        /// <remarks>Estimators will utilize Bloom filters with a capacity set to the estimated number of differences, but then add the whole set. This results in much higher count values than a Bloom filter with a capacity equal to the set size would deal with.</remarks>
        IInvertibleBloomFilter<TEntity, TId, int> CreateHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, TId, int, int, int> bloomFilterConfiguration,
            IHybridEstimatorData<int, int> estimator,
            IHybridEstimatorData<int, int> otherEstimator,
            float? errorRate = null,
            bool destructive = false)
            where TId : struct;

        /// <summary>
        /// Create an invertible Bloom filter based upon the received Bloom filter data.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="bloomFilterConfiguration"></param>
        /// <param name="capacity"></param>
        /// <param name="invertibleBloomFilterData"></param>
        /// <returns></returns>
        /// <remarks>Estimators will utilize Bloom filters with a capacity set to the estimated number of differences, but then add the whole set. This results in much higher count values than a Bloom filter with a capacity equal to the set size would deal with.</remarks>
        IInvertibleBloomFilter<TEntity, TId, int> CreateMatchingHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, TId, int, int, int> bloomFilterConfiguration,
            long capacity,
            IInvertibleBloomFilterData<TId, int, int> invertibleBloomFilterData)
            where TId : struct;

        /// <summary>
        /// Create a Bloom filter.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="bloomFilterConfiguration"></param>
        /// <param name="capacity"></param>
        /// <param name="errorRate"></param>
        /// <returns></returns>
        IInvertibleBloomFilter<TEntity, TId, sbyte> Create<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, TId, int, int, sbyte> bloomFilterConfiguration,
            long capacity,
            float? errorRate = null)
            where TId : struct;
    }
}