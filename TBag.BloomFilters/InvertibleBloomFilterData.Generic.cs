using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TBag.BloomFilters
{
    [DataContract, Serializable]
    public class InvertibleBloomFilterData<TId> : IInvertibleBloomFilterData<TId>
    {
        [DataMember(Order=1)]
        public long BlockSize { get; set; }

        [DataMember(Order = 2)]
        public uint HashFunctionCount { get; set; }

        [DataMember(Order = 3)]
        public TId[] IdSums { get; set; }

        [DataMember(Order = 4)]
        public int[] HashSums { get; set; }

        [DataMember(Order = 5)]
        public int[] Counts { get; set; }
    }
}
