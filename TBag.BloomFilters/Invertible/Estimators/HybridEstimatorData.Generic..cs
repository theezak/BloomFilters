namespace TBag.BloomFilters.Invertible.Estimators
{
    using BloomFilters.Estimators;
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Implementation of <see cref="IHybridEstimatorData{TId,TCount}"/>
    /// </summary>
    /// <typeparam name="TId">The type of the entity identifier</typeparam>
    /// <typeparam name="TCount">Thetype of the occurence count</typeparam>
    [DataContract, Serializable]
    public class HybridEstimatorData<TId,TCount> : IHybridEstimatorData<TId, TCount> 
        where TCount : struct
        where TId : struct
    {
        /// <summary>
        /// Estimated number of elements in the set.
        /// </summary>
        [DataMember(Order = 1)]
        public long ItemCount
        {
            get; set;
        }

        /// <summary>
        /// The strate estimator data
        /// </summary>
        [DataMember(Order = 2)]
        public StrataEstimatorData<TId,TCount> StrataEstimator { get; set; }

        /// <summary>
        /// The bit minwise estimator data.
        /// </summary>
        [DataMember(Order = 3)]
        public BitMinwiseHashEstimatorData BitMinwiseEstimator { get; set; }

        #region Implementation of IStrataEstimatorData{Tid, TCount}
        /// <summary>
        /// Strata estimator data as <see cref="IStrataEstimatorData{TId, TCount}"/>
        /// </summary>
        IStrataEstimatorData<TId, TCount> IHybridEstimatorData<TId, TCount>.StrataEstimator => StrataEstimator;

        /// <summary>
        /// The b-bit minwise estimator data as <see cref="IBitMinwiseHashEstimatorData"/>
        /// </summary>
        IBitMinwiseHashEstimatorData IHybridEstimatorData<TId, TCount>.BitMinwiseEstimator => BitMinwiseEstimator;

        #endregion
    }
}
