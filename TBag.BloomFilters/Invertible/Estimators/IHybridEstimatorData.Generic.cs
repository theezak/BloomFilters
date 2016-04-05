namespace TBag.BloomFilters.Invertible.Estimators
{
    using BloomFilters.Estimators;

    /// <summary>
    /// Interface for hybrid estimator data.
    /// </summary>
    /// <typeparam name="TId">The type of the entity identifier.</typeparam>
    /// <typeparam name="TCount">The type of the occurence count for the invertible Bloom filters.</typeparam>
    public interface IHybridEstimatorData<TId, TCount>
        where TId : struct
        where TCount : struct
    {
        /// <summary>
        ///The number of items in the set.
        /// </summary>
        long ItemCount { get; set; }

        /// <summary>
        /// Data for the strata estimator component of the hybrid estimator.
        /// </summary>
        IStrataEstimatorData<TId, TCount> StrataEstimator { get; }

        /// <summary>
        /// Data for the bit minwise estimator component of the hybrid estimator.
        /// </summary>
        IBitMinwiseHashEstimatorData BitMinwiseEstimator { get;  }
    }
}