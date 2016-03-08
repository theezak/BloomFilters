using System;
using System.Runtime.Serialization;

namespace TBag.BloomFilters
{ 
    [DataContract, Serializable]
    public class HybridEstimatorData<TId,TCount>
        where TCount : struct
    {
        /// <summary>
        /// The capacity
        /// </summary>
        [DataMember(Order = 1)]
        public long Capacity { get; set; }

        [DataMember(Order = 2)]
        public StrataEstimatorData<TId,TCount> StrataEstimator { get; set; }

        [DataMember(Order = 3)]
        public int StrataCount { get; set; }

        [DataMember(Order = 4)]
        public BitMinwiseHashEstimatorData BitMinwiseEstimator { get; set; }
    }
}
