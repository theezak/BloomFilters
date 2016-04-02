namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Interface for a hybrid estimator.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity stored in the estimator</typeparam>
    /// <typeparam name="TId">Type of the entity identifier.</typeparam>
    /// <typeparam name="TCount">Type of the occurence counter in the invertible Bloom filter.</typeparam>
    public interface IHybridEstimator<TEntity, TId, TCount>
        where TId : struct
        where TCount : struct
    {
        /// <summary>
        /// Block size
        /// </summary>
        long BlockSize { get; }

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
        /// Estimate the difference with the given estimator.
        /// </summary>
        /// <param name="estimator"></param>
        /// <param name="destructive">When <c>true</c> the values in this estimator will be altered and rendered useless, else <c>false</c>.</param>
        /// <returns>An estimated number of items that are different.</returns>
        long? Decode(IHybridEstimator<TEntity, TId, TCount> estimator, bool destructive = false);

        /// <summary>
        /// Estimate the difference with the given estimator.
        /// </summary>
        /// <param name="estimator"></param>
        /// <param name="destructive">When <c>true</c> the values in this estimator will be altered and rendered useless, else <c>false</c>.</param>
        /// <returns>An estimated number of items that are different.</returns>
        long? Decode(IHybridEstimatorData<TId, TCount> estimator, bool destructive = false);

        /// <summary>
        /// Extract a serializable version of the estimator data.
        /// </summary>
        /// <returns></returns>
        IHybridEstimatorData<TId, TCount> Extract();

        /// <summary>
        /// Extract a serializable full version of the estimator data.
        /// </summary>
        /// <returns></returns>
        IHybridEstimatorFullData<TId, TCount> FullExtract();

        /// <summary>
        /// Set the data for the hybrid estimator.
        /// </summary>
        /// <returns></returns>
        void Rehydrate(IHybridEstimatorFullData<TId, TCount> data);

        /// <summary>
        /// Fold the strata estimator by the given <paramref name="factor"/>.
        /// </summary>
        /// <param name="factor">Folding factor</param>
        /// <param name="inPlace">When <c>true</c> the estimator is replaced by the folded version, else <c>false</c></param>
        /// <returns>The estimator</returns>
        /// <exception cref="ArgumentException">When the estimator cannot be folded by the given factor.</exception>
        IHybridEstimator<TEntity, TId, TCount> Fold(uint factor, bool inPlace);

        /// <summary>
        /// Compress the estimator.
        /// </summary>
        IHybridEstimator<TEntity, TId, TCount> Compress(bool inPlace);

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="capacity">The capacity (number of items to be added)</param>
        /// <param name="bitSize">The bit size for the bit minwise estimator</param>
        /// <param name="minWiseHashCount">The minwise hash count</param>
        void Initialize(
            long capacity,
            byte bitSize,
            int minWiseHashCount);

        /// <summary>
        /// Remove an item.
        /// </summary>
        /// <param name="item"></param>
        /// <remarks>The bit minwise estimator does not support removal, so removing an item from the estimator disables the bit minwise estimator and thus impacts the estimate.</remarks>
        void Remove(TEntity item);
    }
}