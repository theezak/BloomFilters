using System;
using System.Runtime.Serialization;

namespace TBag.BloomFilters
{
    [DataContract, Serializable]
    public class StrataEstimatorData<TId> : IStrataEstimatorData<TId>
    {
        /// <summary>
        /// The capacity
        /// </summary>
        [DataMember(Order = 1)]
        public ulong Capacity { get; set; }

        [DataMember(Order = 2)]
        public IInvertibleBloomFilterData<TId>[] BloomFilters { get; set; }
    }
}
