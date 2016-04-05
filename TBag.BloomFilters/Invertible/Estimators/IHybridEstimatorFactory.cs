namespace TBag.BloomFilters.Invertible.Estimators
{
    using Configurations;

    /// <summary>
    /// Interface for the hybrid estimator factory.
    /// </summary>
    /// <remarks>Utilizing empirical rules, this factory creates a hybrid estimator of a 'best guess' size.</remarks>
    public interface IHybridEstimatorFactory
    {
        /// <summary>
        /// Create a hybrid estimator, utilizing the provided Bloom filter configuration and estimated set size.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity stored in the estimator.</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the occurence counter for the invertible Bloom filter.</typeparam>        
        /// <param name="configuration">Bloom filter configuration</param>
        /// <param name="setSize">Estimated total count of the items to be added to the estimator.</param>
        /// <param name="failedDecodeCount">Optional parameter indicating the number of failed decodes.</param>
        /// <returns>A hybrid estimator</returns>
        /// <remarks>If decoding the invertible Bloom filter fails, a better hybrid estimator can be created by either providing a larger value for <paramref name="setSize"/> and/or providing a value for <paramref name="failedDecodeCount"/> (which will trigger an empirical rule for increasing the estimator size).</remarks>
        HybridEstimator<TEntity, TId, TCount> Create<TEntity, TId, TCount>(
             IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
             long setSize,
             byte failedDecodeCount = 0)
             where TCount : struct
             where TId : struct;

        /// <summary>
        /// Create a hybrid estimator that matches the given hybrid estimator data.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the occurrence counter</typeparam>        
        /// <param name="data">Hybrid estimator data from another set.</param>
        /// <param name="configuration">Bloom filter configuration</param>
        /// <param name="setSize">The size of the set to estimate (your local set)</param>
        /// <returns></returns>
        /// <remarks>The set size is for the your local set. The <paramref name="data"/> would typically be for the set that you are comparing against.</remarks>
        IHybridEstimator<TEntity, int, TCount> CreateMatchingEstimator<TEntity, TId, TCount>(
            IHybridEstimatorData<int, TCount> data,
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            long setSize)
            where TId : struct
            where TCount : struct;

        /// <summary>
        /// Extract the hybrid estimator data from the <paramref name="precalculatedEstimator">pre-calculated estimator</paramref> in a compressed size
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of occurence count.</typeparam>
        /// <param name="configuration">Bloom filter configuration</param>
        /// <param name="precalculatedEstimator">The pre-calculated estimator.</param>
        /// <param name="failedDecodeCount">Number of times decoding has failed based upon the provided estimator.</param>
        /// <returns>The data for <paramref name="precalculatedEstimator"/> folded down to a size appropriate for the actual number of items in the estimator.</returns>
        IHybridEstimatorData<int, TCount> Extract<TEntity, TId, TCount>(
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
             HybridEstimator<TEntity, TId, TCount> precalculatedEstimator,
            byte failedDecodeCount = 0)
            where TCount : struct
            where TId : struct;

        /// <summary>
        /// Get the recommended strata count.
        /// </summary>
        /// <typeparam name="TEntity">The entity to add</typeparam>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <typeparam name="THash">The hash type</typeparam>
        /// <typeparam name="TCount">The occurence count type.</typeparam>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="setSize">Number of items to be added</param>
        /// <param name="failedDecodeCount">Number of times the estimator has failed to decode.</param>
        /// <returns>The recommended number of Bloom filters in the estimator</returns>
        byte GetRecommendedStrata<TEntity, TId, THash, TCount>(
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
           long setSize,
           byte failedDecodeCount = 0)
            where TId : struct
           where THash : struct
           where TCount : struct;


        /// <summary>
        /// Get the recommended bit size.
        /// </summary>
        /// <typeparam name="TEntity">The entity to add</typeparam>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <typeparam name="THash">The hash type</typeparam>
        /// <typeparam name="TCount">The occurence count type.</typeparam>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="setSize">Number of items to be added</param>
        /// <param name="failedDecodeCount">Number of times the estimator has failed to decode.</param>
        /// <returns>The recommended number of bits in the bit minwise estimator.</returns>
        byte GetRecommendedBitSize<TEntity, TId, THash, TCount>(
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
      long setSize,
      byte failedDecodeCount = 0)
       where TId : struct
      where THash : struct
      where TCount : struct;

        /// <summary>
        /// Get the recommended minwise hash count.
        /// </summary>
        /// <typeparam name="TEntity">The entity to add</typeparam>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <typeparam name="THash">The hash type</typeparam>
        /// <typeparam name="TCount">The occurence count type.</typeparam>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="setSize">Number of items to be added</param>
        /// <param name="failedDecodeCount">Number of times the estimator has failed to decode.</param>
        /// <returns>The recommended number of hash functions for bit minwise estimator.</returns>
        int GetRecommendedMinwiseHashCount<TEntity, TId, THash, TCount>(
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
          long setSize,
          byte failedDecodeCount = 0)
           where TId : struct
          where THash : struct
          where TCount : struct;

        /// <summary>
        /// Determine the size of the estimator based upon the number of elements and the number of failed attempts.
        /// </summary>
        /// <typeparam name="TEntity">The entity to add</typeparam>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <typeparam name="THash">The hash type</typeparam>
        /// <typeparam name="TCount">The occurence count type.</typeparam>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="setSize">Number of items to be added</param>
        /// <param name="failedDecodeCount">Number of times the estimator has failed to decode.</param>
        /// <returns>The recommended capacity for the Bloom filters inside the estimator.</returns>
        long GetRecommendedBlockSize<TEntity, TId, THash, TCount>(
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
            long setSize,
            byte failedDecodeCount = 0)
            where TId : struct
            where THash : struct
            where TCount : struct;
    }
}