namespace TBag.BloomFilters.Estimators
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Serializable strata estimator data.
    /// </summary>
    /// <typeparam name="TId">The type of the identifer</typeparam>
    /// <typeparam name="TCount">The type of the occurence count</typeparam>
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

        /// <summary>
        /// The desired error rate for the IBF.
        /// </summary>
        [DataMember(Order = 4)]
        public float ErrorRate { get; set; }

        /// <summary>
        /// The number of hash functions to use.
        /// </summary>
        [DataMember(Order = 5)]
        public uint HashFunctionCount { get; set; }

        #region Implementation of IStrataEstimatorData{TId,TCount}
        /// <summary>
        /// The Bloom filters as <see cref="IInvertibleBloomFilterData{TId, TEntityHash, TCount}"/>
        /// </summary>
        IInvertibleBloomFilterData<TId, int, TCount>[] IStrataEstimatorData<TId, TCount>.BloomFilters => BloomFilters;
        #endregion
    }
}
