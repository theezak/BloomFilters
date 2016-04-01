namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Interface for hybrid estimator data.
    /// </summary>
    /// <typeparam name="TId">The type of the entity identifier.</typeparam>
    /// <typeparam name="TCount">The type of the occurence count for the invertible Bloom filters.</typeparam>
    public interface IHybridEstimatorFullData<TId, TCount>
        where TId : struct
        where TCount : struct
    {
        /// <summary>
        /// Estimated size of the set.
        /// </summary>
        long ItemCount { get; set; }

        /// <summary>
        /// The capacity
        /// </summary>
        long BlockSize { get; set; }

        /// <summary>
        /// Data for the strata estimator component of the hybrid estimator.
        /// </summary>
        IStrataEstimatorData<TId, TCount> StrataEstimator { get; }

        /// <summary>
        /// The strata count
        /// </summary>
        /// <remarks>The hybrid estimator knows a cut off, where the less selective strata go to a minwise estimator. This is the value of that cut off.</remarks>
        byte StrataCount { get; set; }

        /// <summary>
        /// Data for the bit minwise estimator component of the hybrid estimator.
        /// </summary>
        IBitMinwiseHashEstimatorFullData BitMinwiseEstimator { get;  }
    }
}