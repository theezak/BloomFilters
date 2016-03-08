using System;
using System.Runtime.Serialization;

namespace TBag.BloomFilters
{
    [DataContract, Serializable]
    public class StrataEstimatorData<TId,TCount> : IStrataEstimatorData<TId, TCount>
        where TCount : struct
    {
        /// <summary>
        /// The capacity
        /// </summary>
        [DataMember(Order = 1)]
        public long Capacity { get; set; }

        [DataMember(Order = 2)]
        public IInvertibleBloomFilterData<TId,TCount>[] BloomFilters { get; set; }
    }
}
