using System;
using System.Runtime.Serialization;

namespace TBag.BloomFilters
{
    [DataContract, Serializable]
    public class BitMinwiseHashEstimatorData
    {
        [DataMember(Order = 1)]
        public byte BitSize { get; set; }

        [DataMember(Order = 2)]
        public ulong Capacity { get; set; }

        [DataMember(Order = 3)]
        public int HashCount { get; set; }

        [DataMember(Order = 4)]
        public byte[] Values { get; set; }
    }
}
