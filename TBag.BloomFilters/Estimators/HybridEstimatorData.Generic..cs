using System;
using System.Runtime.Serialization;

namespace TBag.BloomFilters.Estimators
{ 
    /// <summary>
    /// Implementation of <see cref="IHybridEstimatorData{TId,TCount}"/>
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TCount"></typeparam>
    [DataContract, Serializable]
    public class HybridEstimatorData<TId,TCount> : IHybridEstimatorData<TId, TCount> where TCount : struct
    {
        /// <summary>
        /// Estimated number of elements in the set.
        /// </summary>
        [DataMember(Order = 1)]
        public long CountEstimate { get; set; }

        /// <summary>
        /// The capacity
        /// </summary>
        [DataMember(Order = 2)]
        public long Capacity { get; set; }

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
        public BitMinwiseHashEstimatorData BitMinwiseEstimator { get; set; }

        IStrataEstimatorData<TId, TCount> IHybridEstimatorData<TId, TCount>.StrataEstimator => StrataEstimator;

        IBitMinwiseHashEstimatorData IHybridEstimatorData<TId, TCount>.BitMinwiseEstimator => BitMinwiseEstimator;     
    }
}
