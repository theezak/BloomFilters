

namespace TBag.BloomFilters.Estimators
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Serializable strata estimator data.
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TCount"></typeparam>
    [DataContract, Serializable]
    public class StrataEstimatorData<TId,TCount> : IStrataEstimatorData<TId, TCount>
        where TCount : struct
        where TId : struct
    {
        /// <summary>
        /// The capacity
        /// </summary>
        [DataMember(Order = 1)]
        public long Capacity { get; set; }

        /// <summary>
        /// The decode factor.
        /// </summary>
        [DataMember(Order = 2)]
        public double DecodeCountFactor { get; set; }

        /// <summary>
        /// The Bloom filters that are part of the strata estimator.
        /// </summary>
        [DataMember(Order = 3)]
        public InvertibleBloomFilterData<TId,int,TCount>[] BloomFilters { get; set; }

        IInvertibleBloomFilterData<TId, int, TCount>[] IStrataEstimatorData<TId, TCount>.BloomFilters => BloomFilters;               
    }
}
