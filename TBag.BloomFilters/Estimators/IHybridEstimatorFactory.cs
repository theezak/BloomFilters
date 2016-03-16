namespace TBag.BloomFilters.Estimators
{
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
        IHybridEstimator<TEntity, int, TCount> Create<TEntity, TId, TCount>(
            IBloomFilterConfiguration<TEntity, TId, int, int, TCount> configuration, 
            long setSize, 
            byte failedDecodeCount = 0)
            where TId : struct
            where TCount : struct;

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
            IBloomFilterConfiguration<TEntity, TId, int,  int, TCount> configuration,
            long setSize)
            where TId : struct
            where TCount : struct;
    }
}