using System.Runtime.Serialization;

namespace TBag.BloomFilters
{
    public class HybridEstimatorData<TId>
    {
        /// <summary>
        /// The capacity
        /// </summary>
        [DataMember(Order = 1)]
        public ulong Capacity { get; set; }

        [DataMember(Order = 2)]
        public StrataEstimatorData<TId> StrataEstimator { get; set; }

        [DataMember(Order = 3)]
        public int StrataCount { get; set; }

        [DataMember(Order = 4)]
        public BitMinwiseHashEstimatorData BitMinwiseEstimator { get; set; }
    }
}
