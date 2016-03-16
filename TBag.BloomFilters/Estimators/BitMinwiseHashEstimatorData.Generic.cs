namespace TBag.BloomFilters.Estimators
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Implementation of <see cref="IBitMinwiseHashEstimatorData"/>.
    /// </summary>
    [DataContract, Serializable]
    public class BitMinwiseHashEstimatorData : IBitMinwiseHashEstimatorData
    {
        /// <summary>
        /// Number of bits used for a single cell.
        /// </summary>
        [DataMember(Order = 1)]
        public byte BitSize { get; set; }

        /// <summary>
        /// Capacity of the esitmator
        /// </summary>
        [DataMember(Order = 2)]
        public ulong Capacity { get; set; }

        /// <summary>
        /// The number of hash functions used.
        /// </summary>
        [DataMember(Order = 3)]
        public int HashCount { get; set; }

        /// <summary>
        /// The values
        /// </summary>
        [DataMember(Order = 4)]
        public byte[] Values { get; set; }
    }
}
