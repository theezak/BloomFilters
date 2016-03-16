namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Interface for strata estimator data.
    /// </summary>
    /// <typeparam name="TId">The type of the entity identifier.</typeparam>
    /// <typeparam name="TCount">The type for the occurence count in the invertible Bloom filter</typeparam>
    public interface IStrataEstimatorData<TId,TCount>
        where TId :struct
        where TCount : struct
    {
        /// <summary>
        /// The bloom filters.
        /// </summary>
        IInvertibleBloomFilterData<TId,int,TCount>[] BloomFilters { get;  }

        /// <summary>
        /// The capacity for the estimator.
        /// </summary>
        long Capacity { get; set; }

        /// <summary>
        /// The decode count factor.
        /// </summary>
        double DecodeCountFactor { get; set; }

        /// <summary>
        /// The desired error rate for the IBF
        /// </summary>
        float ErrorRate { get; set; }

        /// <summary>
        /// The number of hash functions to use.
        /// </summary>
        uint HashFunctionCount { get; set; }
    }
}