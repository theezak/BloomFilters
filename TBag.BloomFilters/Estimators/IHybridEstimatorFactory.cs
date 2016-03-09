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
        /// <typeparam name="TId">The type of the entity identifier.</typeparam>
        /// <typeparam name="TCount">The type of the occurence counter for the invertible Bloom filter.</typeparam>
        /// <param name="configuration">Bloom filter configuration</param>
        /// <param name="setSize">Estimated total count of the items to be added to the estimator.</param>
        /// <param name="failedDecodeCount">Optional parameter indicating the number of failed decodes.</param>
        /// <returns>A hybrid estimator</returns>
        /// <remarks>If decoding the invertible Bloom filter fails, a better hybrid estimator can be created by either providing a larger value for <paramref name="setSize"/> and/or providing a value for <paramref name="failedDecodeCount"/> (which will trigger an empirical rule for increasing the estimator size).</remarks>
        HybridEstimator<TEntity, TId, TCount> Create<TEntity, TId, TCount>(
            IBloomFilterConfiguration<TEntity, int, TId, long, TCount> configuration, 
            ulong setSize, 
            byte failedDecodeCount = 0) where TCount : struct;
    }
}