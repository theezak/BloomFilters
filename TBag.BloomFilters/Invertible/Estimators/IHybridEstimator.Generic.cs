namespace TBag.BloomFilters.Invertible.Estimators
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
        /// The actual block size for a single strata.
        /// </summary>
        long BlockSize { get; }

        /// <summary>
        /// Block size that the estimator behaves to.
        /// </summary>
        long VirtualBlockSize { get; }

        /// <summary>
        /// The number of hash functions used.
        /// </summary>
        uint HashFunctionCount { get; }

        /// <summary>
        /// The error rate
        /// </summary>
        float ErrorRate { get; }

        /// <summary>
        /// The hybrid estimator item count.
        /// </summary>
        long ItemCount { get; }

        /// <summary>
        /// The decode count factor.
        /// </summary>
        double DecodeCountFactor { get; set; }

        /// <summary>
        /// Add an item to the estimator,
        /// </summary>
        /// <param name="item">The item to add</param>
        void Add(TEntity item);

        /// <summary>
        /// Determine if the estimator contains the item
        /// </summary>
        /// <param name="item"></param>
        /// <remarks>Except a much higher false positive and false negative rate.</remarks>
        bool Contains(TEntity item);

        /// <summary>
        /// Estimate the difference with the given estimator.
        /// </summary>
        /// <param name="estimator"></param>
        /// <param name="destructive">When <c>true</c> the values in this estimator will be altered and rendered useless, else <c>false</c>.</param>
        /// <returns>An estimated number of items that are different.</returns>
        long? Decode(IHybridEstimator<TEntity, TId, TCount> estimator, 
            bool destructive = false);

        /// <summary>
        /// Decode the given hybrid estimator data.
        /// </summary>
        /// <param name="estimator">The estimator for the other set.</param>
        /// <param name="destructive">When <c>true</c> the values in this estimator will be altered and rendered useless, else <c>false</c>.</param>
        /// <returns>An estimate of the number of differences between the two sets that the estimators are based upon.</returns>
        long? Decode(IHybridEstimatorData<int, TCount> estimator,
            bool destructive = false);

        /// <summary>
        /// Intersect with the given estimator
        /// </summary>
        /// <param name="estimator"></param>
        void Intersect(IHybridEstimator<TEntity, TId, TCount> estimator);

        /// <summary>
        /// Intersect with the given estimator
        /// </summary>
        /// <param name="estimator"></param>
        void Intersect(IHybridEstimatorFullData<int, TCount> estimator);

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
      /// Rehydrate the hybrid estimator 
      /// </summary>
      /// <param name="data">The data to restore</param>
      /// <remarks>This rehydrate is lossy, since it can't restore the bit minwise estimator.</remarks>
      void Rehydrate(IHybridEstimatorData<int, TCount> data);

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