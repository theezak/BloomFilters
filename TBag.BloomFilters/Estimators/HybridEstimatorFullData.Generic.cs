namespace TBag.BloomFilters.Estimators
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Implementation of <see cref="IHybridEstimatorData{TId,TCount}"/>
    /// </summary>
    /// <typeparam name="TId">The type of the entity identifier</typeparam>
    /// <typeparam name="TCount">Thetype of the occurence count</typeparam>
    [DataContract, Serializable]
    public class HybridEstimatorFullData<TId,TCount> : IHybridEstimatorFullData<TId, TCount> 
        where TCount : struct
        where TId : struct
    {
        /// <summary>
        /// Estimated number of elements in the set.
        /// </summary>
        public long ItemCount => (StrataEstimator?.ItemCount ?? 0L) + (BitMinwiseEstimator?.ItemCount ?? 0L);

        /// <summary>
        /// The capacity
        /// </summary>
        [DataMember(Order = 2)]
        public long BlockSize { get; set; }

        /// <summary>
        /// The strate estimator data
        /// </summary>
        [DataMember(Order = 3)]
        public StrataEstimatorData<TId,TCount> StrataEstimator { get; set; }

        /// <summary>
        /// The number of strata.
        /// </summary>
        [DataMember(Order = 4)]
        public byte StrataCount { get; set; }

        /// <summary>
        /// The bit minwise estimator data.
        /// </summary>
        [DataMember(Order = 5)]
        public BitMinwiseHashEstimatorFullData BitMinwiseEstimator { get; set; }

        #region Implementation of IStrataEstimatorData{Tid, TCount}
        /// <summary>
        /// Strata estimator data as <see cref="IStrataEstimatorData{TId, TCount}"/>
        /// </summary>
        IStrataEstimatorData<TId, TCount> IHybridEstimatorFullData<TId, TCount>.StrataEstimator => StrataEstimator;

        /// <summary>
        /// The b-bit minwise estimator data as <see cref="IBitMinwiseHashEstimatorData"/>
        /// </summary>
        IBitMinwiseHashEstimatorFullData IHybridEstimatorFullData<TId, TCount>.BitMinwiseEstimator => BitMinwiseEstimator;
        #endregion
    }
}
