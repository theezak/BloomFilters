namespace TBag.BloomFilters.Invertible.Estimators
{
    using System;

    /// <summary>
    /// Interface for the Strata estimator.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The type of the entity identifier.</typeparam>
    /// <typeparam name="TCount">The type of the occurence count for the Bloom filter.</typeparam>
    public interface IStrataEstimator<TEntity, TId, TCount> 
        where TCount : struct
        where TId : struct
    {
        /// <summary>
        /// The block size
        /// </summary>
        long BlockSize { get; }

        /// <summary>
        /// The decode count factor.
        /// </summary>
        double DecodeCountFactor { get; set; }

        /// <summary>
        /// The error rate
        /// </summary>
        float ErrorRate { get; }

        /// <summary>
        /// The number of items in the estimator.
        /// </summary>
        long ItemCount { get; }

        /// <summary>
        /// The hash function count
        /// </summary>
        uint HashFunctionCount { get; }

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
        /// <returns>Estimated difference as the number of elements, or <c>null</c> when the estimate fails.</returns>
        long? Decode(IStrataEstimator<TEntity, TId, TCount> estimator,
            bool destructive = false);

         /// <summary>
        /// Extract the serializable data from the estimator.
        /// </summary>
        /// <returns></returns>
        StrataEstimatorData<TId, TCount> Extract();

        /// <summary>
        /// Rehydrate the data
        /// </summary>
        /// <param name="data"></param>
        void Rehydrate(IStrataEstimatorData<int, TCount> data);

        /// <summary>
        /// Remove an item from the estimator
        /// </summary>
        /// <param name="item"></param>
        void Remove(TEntity item);

        /// <summary>
        /// Fold the strata estimator by the given <paramref name="factor"/>.
        /// </summary>
        /// <param name="factor">Folding factor</param>
        /// <param name="inPlace">When <c>true</c> the estimator is replaced by the folded version, else <c>false</c></param>
        /// <returns>The estimator</returns>
        /// <exception cref="ArgumentException">When the estimator cannot be folded by the given factor.</exception>
        IStrataEstimator<TEntity, TId, TCount> Fold(uint factor, bool inPlace = false);

        /// <summary>
        /// Compress the estimator.
        /// </summary>
        IStrataEstimator<TEntity, TId, TCount> Compress(bool inPlace = false);

        /// <summary>
        /// Determine if an item is in the estimator
        /// </summary>
        /// <param name="item"></param>
        /// <returns><c>null</c> when the strata estimator can't determine membership (strata for the item is above the maximum strata), otherwise <c>true</c> when a member, or <c>false</c> when not a member.</returns>
        bool? Contains(TEntity item);
    }
}