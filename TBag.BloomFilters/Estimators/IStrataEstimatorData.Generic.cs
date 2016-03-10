namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Interface for strata estimator data.
    /// </summary>
    /// <typeparam name="TId">The type of the entity identifier.</typeparam>
    /// <typeparam name="TCount">The type for the occurence count in the invertible Bloom filter</typeparam>
    public interface IStrataEstimatorData<TId,TCount>
        where TCount : struct
    {
        /// <summary>
        /// The bloom filters.
        /// </summary>
        IInvertibleBloomFilterData<TId,TCount>[] BloomFilters { get;  }

        /// <summary>
        /// The capacity for the estimator.
        /// </summary>
        long Capacity { get; set; }

        double DecodeCountFactor { get; set; }
    }
}