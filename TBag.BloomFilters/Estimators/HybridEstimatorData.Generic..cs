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
        /// The capacity
        /// </summary>
        [DataMember(Order = 1)]
        public long Capacity { get; set; }

        [DataMember(Order = 2)]
        public StrataEstimatorData<TId,TCount> StrataEstimator { get; set; }

        [DataMember(Order = 3)]
        public byte StrataCount { get; set; }

        [DataMember(Order = 4)]
        public BitMinwiseHashEstimatorData BitMinwiseEstimator { get; set; }

        IStrataEstimatorData<TId, TCount> IHybridEstimatorData<TId, TCount>.StrataEstimator => StrataEstimator;

        IBitMinwiseHashEstimatorData IHybridEstimatorData<TId, TCount>.BitMinwiseEstimator => BitMinwiseEstimator;     
    }
}
