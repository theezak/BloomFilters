

namespace TBag.BloomFilters.Standard
{

    using System;
    using System.Runtime.Serialization;

    [Serializable,DataContract]
    internal class BloomFilterData
    {
        [DataMember(Order =1)]
        public long BlockSize { get; set; }

        [DataMember(Order =2)]
        public long Capacity { get; set; }

        [DataMember(Order =3)]
        public uint HashFunctionCount { get; set; }

        [DataMember(Order =4)]
        public byte[] Bits { get; set; }
    }
}
