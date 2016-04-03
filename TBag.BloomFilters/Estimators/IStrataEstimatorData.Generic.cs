using TBag.BloomFilters.Invertible;

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
        /// The strata indexes for the Bloom filters.
        /// </summary>
        /// <remarks>used as a work around for serializers that ignore null values.</remarks>
        byte[] BloomFilterStrataIndexes { get; set; }

        /// <summary>
        /// The number of stratas.
        /// </summary>
        byte StrataCount { get; set; }

        /// <summary>
        /// The capacity for the estimator.
        /// </summary>
        long BlockSize { get; set; }

        /// <summary>
        /// The decode count factor.
        /// </summary>
        double DecodeCountFactor { get; set; }

        /// <summary>
        /// The item count
        /// </summary>
        long ItemCount { get; }
    }
}