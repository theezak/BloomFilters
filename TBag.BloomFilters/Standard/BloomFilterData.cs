

namespace TBag.BloomFilters.Standard
{

    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Data for a Bloom filter
    /// </summary>
    [Serializable,DataContract]
    public class BloomFilterData : IBloomFilterData
    {
        /// <summary>
        /// The block size
        /// </summary>
        [DataMember(Order =1)]
        public long BlockSize { get; set; }

        /// <summary>
        /// The capacity
        /// </summary>
        [DataMember(Order =2)]
        public long Capacity { get; set; }

        /// <summary>
        /// Number of hash functions used
        /// </summary>
        [DataMember(Order =3)]
        public uint HashFunctionCount { get; set; }

        /// <summary>
        /// The item count
        /// </summary>
        [DataMember(Order=4)]
        public long ItemCount { get; set; }

        /// <summary>
        /// The bits
        /// </summary>
        [DataMember(Order =5)]
        public byte[] Bits { get; set; }
    }
}
