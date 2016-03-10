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
        /// The decode count factor.
        /// </summary>
        double DecodeCountFactor { get; set; }

        /// <summary>
        /// Add an item to the estimator,
        /// </summary>
        /// <param name="item"></param>
        void Add(TEntity item);

        /// <summary>
        /// Decode utilizing the given estimator, returning an estimate for the difference
        /// </summary>
        /// <param name="estimator"></param>
        /// <param name="destructive">When <c>true</c> the values in this estimator will be altered and rendered useless, else <c>false</c>.</param>
        /// <returns>Estimated difference as the number of elements.</returns>
        ulong Decode(IStrataEstimator<TEntity, TId, TCount> estimator,
            bool destructive = false);

        /// <summary>
        /// Decode utilizing the given estimator, returning an estimate for the difference
        /// </summary>
        /// <param name="estimator"></param>
        /// <param name="destructive">When <c>true</c> the values in this estimator will be altered and rendered useless, else <c>false</c>.</param>
        /// <returns>Estimated difference as the number of elements.</returns>
        ulong Decode(IStrataEstimatorData<TId, TCount> estimator,
            bool destructive = false);

        /// <summary>
        /// Extract the serializable data from the estimator.
        /// </summary>
        /// <returns></returns>
        StrataEstimatorData<TId, TCount> Extract();

        /// <summary>
        /// Remove an item from the estimator
        /// </summary>
        /// <param name="item"></param>
        void Remove(TEntity item);
    }
}