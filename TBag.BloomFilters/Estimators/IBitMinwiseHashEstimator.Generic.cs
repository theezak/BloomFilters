using System;

namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Interface for a bit minwise hash estimator
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <typeparam name="TId">The type of the entity identifier</typeparam>
    /// <typeparam name="TCount">The type of the occurence counter.</typeparam>
    public interface IBitMinwiseHashEstimator<TEntity, TId, TCount> 
        where TCount : struct
        where TId : struct
    {
        /// <summary>
        /// Add an item to the estimator
        /// </summary>
        /// <param name="item">The entity to add</param>
        void Add(TEntity item);

        /// <summary>
        /// Add the <paramref name="estimator">estimator</paramref> to the current estimator.
        /// </summary>
        /// <param name="estimator">Estimator to add</param>
        /// <returns></returns>
        void Add(IBitMinwiseHashEstimator<TEntity, TId, TCount> estimator);

        /// <summary>
        /// Add the <paramref name="estimator">estimator</paramref> to the current estimator.
        /// </summary>
        /// <param name="estimator">Estimator to add</param>
         /// <returns></returns>
        void Add(IBitMinwiseHashEstimatorFullData estimator);

        /// <summary>
        /// Extract a serializable version of the bit minwise hash estimator.
        /// </summary>
        /// <returns></returns>
        BitMinwiseHashEstimatorData Extract();

        /// <summary>
        /// Fold the estimator.
        /// </summary>
        /// <param name="factor">Factor to fold by.</param>
        /// <param name="inPlace">When <c>true</c> the estimator will be replaced by a folded estimator, else <c>false</c>.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">When the estimator cannot be folded by the given factor.</exception>
        IBitMinwiseHashEstimator<TEntity, TId, TCount> Fold(uint factor, bool inPlace = false);

        /// <summary>
        /// Compress the estimator,
        /// </summary>
        /// <param name="inPlace"></param>
        /// <returns></returns>
        IBitMinwiseHashEstimator<TEntity, TId, TCount> Compress(bool inPlace = false);

        /// <summary>
        /// Full extract of the data
        /// </summary>
        /// <returns></returns>
        BitMinwiseHashEstimatorFullData FullExtract();

        /// <summary>
        /// Determine the similarity between the hash estimator and the provided hash estimator data.
        /// </summary>
        /// <param name="estimatorData">The estimator data to compare to.</param>
        /// <returns>Similarity (percentage similar, zero is completely different, one is completely the same)</returns>
        /// <remarks>Zero is no similarity, one is completely similar.</remarks>
        double? Similarity(IBitMinwiseHashEstimatorData estimatorData);

        /// <summary>
        /// Determine the similarity with the provided hash estimator.
        /// </summary>
        /// <param name="estimator">The estimator to compare to.</param>
        /// <returns>Similarity (percentage similar, zero is completely different, one is completely the same)</returns>
        /// <remarks>Zero is no similarity, one is completely similar.</remarks>
        double? Similarity(IBitMinwiseHashEstimator<TEntity, TId, TCount> estimator);

        /// <summary>
        /// The number of items in the estimator.
        /// </summary>
        long ItemCount { get; }
    }
}