namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Interface for the Strata estimator.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The type of the entity identifier.</typeparam>
    /// <typeparam name="TCount">The type of the occurence count for the Bloom filter.</typeparam>
    public interface IStrataEstimator<TEntity, TId, TCount> where TCount : struct
    {
        /// <summary>
        /// Add an item to the estimator,
        /// </summary>
        /// <param name="item"></param>
        void Add(TEntity item);

        /// <summary>
        /// Decode utilizing the given estimator, provind an estimate for the difference
        /// </summary>
        /// <param name="estimator"></param>
        /// <returns>Estimated difference as the number of elements.</returns>
        ulong Decode(IStrataEstimator<TEntity, TId, TCount> estimator);

        /// <summary>
        /// Decode utilizing the given estimator, provind an estimate for the difference
        /// </summary>
        /// <param name="estimator"></param>
        /// <returns>Estimated difference as the number of elements.</returns>
        ulong Decode(IStrataEstimatorData<TId, TCount> estimator);

        /// <summary>
        /// Extract the serializable data from the estimator.
        /// </summary>
        /// <returns></returns>
        IStrataEstimatorData<TId, TCount> Extract();

        /// <summary>
        /// Remove an item from the estimator
        /// </summary>
        /// <param name="item"></param>
        void Remove(TEntity item);
    }
}