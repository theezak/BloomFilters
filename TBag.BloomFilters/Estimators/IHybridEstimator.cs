namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Interface for a hybrid estimator.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity stored in the estimator</typeparam>
    /// <typeparam name="TId">Type of the entity identifier.</typeparam>
    /// <typeparam name="TCount">Type of the occurence counter in the invertible Bloom filter.</typeparam>
    public interface IHybridEstimator<TEntity, TId, TCount> 
        where TCount : struct
    {
        /// <summary>
        /// Add an item to the estimator,
        /// </summary>
        /// <param name="item"></param>
        void Add(TEntity item);

        /// <summary>
        /// Estimate the difference with the given estimator.
        /// </summary>
        /// <param name="estimator"></param>
        /// <returns>An estimated number of items that are different.</returns>
        ulong Decode(IHybridEstimator<TEntity, TId, TCount> estimator);

        /// <summary>
        /// Estimate the difference with the given estimator.
        /// </summary>
        /// <param name="estimator"></param>
        /// <returns>An estimated number of items that are different.</returns>
        ulong Decode(IHybridEstimatorData<TId, TCount> estimator);

        /// <summary>
        /// Extract a serializable version of the estimator data.
        /// </summary>
        /// <returns></returns>
        IHybridEstimatorData<TId, TCount> ExtractHybrid();
    }
}