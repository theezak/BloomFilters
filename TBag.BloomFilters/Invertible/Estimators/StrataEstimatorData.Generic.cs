namespace TBag.BloomFilters.Invertible.Estimators
{
    using Invertible;
    using System;
    using System.Runtime.Serialization;
    using System.Linq;

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
        public long BlockSize { get; set; }

        /// <summary>
        /// The decode factor.
        /// </summary>
        [DataMember(Order = 2)]
        public double DecodeCountFactor { get; set; }

        /// <summary>
        /// The number of stratas.
        /// </summary>
        [DataMember(Order = 3)]
        public byte StrataCount { get; set; }

        /// <summary>
        /// The number of hash functions used.
        /// </summary>
        [DataMember(Order = 4)]
        public uint HashFunctionCount { get; set; }

        /// <summary>
        /// The Bloom filters that are part of the strata estimator.
        /// </summary>
        [DataMember(Order = 5)]
        public InvertibleBloomFilterData<TId,int,TCount>[] BloomFilters { get; set; }

        /// <summary>
        /// The strata indexes for the Bloom filters.
        /// </summary>
        /// <remarks>used as a work around for serializers that ignore null values.</remarks>
        [DataMember(Order = 6)]
        public byte[] BloomFilterStrataIndexes { get; set; }

        /// <summary>
        /// The item count
        /// </summary>
        public long ItemCount => BloomFilters?.Sum(filter => filter?.ItemCount ?? 0L)??0L;

        /// <summary>
        /// The item count up to the given <paramref name="strata"/>.
        /// </summary>
        /// <param name="strata"></param>
        /// <returns></returns>
        public long StrataItemCount(byte strata)
        {
            return Enumerable.Range(0, strata).Sum(i => this.GetFilterForStrata(i)?.ItemCount ?? 0L);
        }

        #region Implementation of IStrataEstimatorData{TId,TCount}
        /// <summary>
        /// The Bloom filters as <see cref="IInvertibleBloomFilterData{TId, TEntityHash, TCount}"/>
        /// </summary>
        IInvertibleBloomFilterData<TId, int, TCount>[] IStrataEstimatorData<TId, TCount>.BloomFilters => BloomFilters;
        #endregion
    }
}
