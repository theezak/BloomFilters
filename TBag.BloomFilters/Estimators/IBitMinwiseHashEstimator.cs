namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Interface for a bit minwise hash estimator
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <typeparam name="TId">The type of the entity identifier</typeparam>
    /// <typeparam name="TCount">The type of the occurence counter.</typeparam>
    public interface IBitMinwiseHashEstimator<TEntity, TId, TCount> where TCount : struct
    {
        /// <summary>
        /// Add an item to the estimator
        /// </summary>
        /// <param name="item"></param>
        void Add(TEntity item);

        /// <summary>
        /// Extract a serializable version of the bit minwise hash estimator.
        /// </summary>
        /// <returns></returns>
        IBitMinwiseHashEstimatorData Extract();

        /// <summary>
        /// Determine the similarity between the hash estimator and the provided hash estimator data.
        /// </summary>
        /// <param name="estimatorData"></param>
        /// <returns>Similarity (percentage similar, zero is completely different, one is completely the same)</returns>
        /// <remarks>Zero is no similarity, one is completely similar.</remarks>
        double Similarity(IBitMinwiseHashEstimatorData estimatorData);

        /// <summary>
        /// Determine the similarity with the provided hash estimator.
        /// </summary>
        /// <param name="estimator"></param>
        /// <returns>Similarity (percentage similar, zero is completely different, one is completely the same)</returns>
        /// <remarks>Zero is no similarity, one is completely similar.</remarks>
        double Similarity(IBitMinwiseHashEstimator<TEntity, TId, TCount> estimator);
    }
}