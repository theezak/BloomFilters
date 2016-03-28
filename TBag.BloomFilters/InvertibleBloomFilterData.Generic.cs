namespace TBag.BloomFilters
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Implementation of <see cref="IInvertibleBloomFilterData{TId, TEntityHash, TCount}"/>
    /// </summary>
    /// <typeparam name="TId">Type of the entity identifier</typeparam>
    /// <typeparam name="THash">Type of the hash</typeparam>
    /// <typeparam name="TCount">Type of the occurence count</typeparam>
    [DataContract, Serializable]
    public class InvertibleBloomFilterData<TId, THash, TCount> :
        IInvertibleBloomFilterData<TId, THash, TCount>
        where TCount : struct
        where THash : struct
        where TId : struct
    {
        /// <summary>
        /// The number of cells for a single hash function.
        /// </summary>
        [DataMember(Order = 2)]
        public long BlockSize { get; set; }

        /// <summary>
        /// The number of hash functions
        /// </summary>
        [DataMember(Order = 3)]
        public uint HashFunctionCount { get; set; }

        /// <summary>
        /// An array of identifier (key) sums.
        /// </summary>
        [DataMember(Order = 4)]
        public TId[] IdSums { get; set; }


        /// <summary>
        /// An array of hash value sums.
        /// </summary>
        [DataMember(Order = 5)]
        public THash[] HashSums { get; set; }

        [DataMember(Order = 6)]
        public TCount[] Counts
        {
            get; set;
        }

        /// <summary>
        /// The data for the reverse IBF
        /// </summary>
        /// <remarks>Only used by the hybrid IBF</remarks>
        [DataMember(Order = 7)]
        public InvertibleBloomFilterData<TId, THash, TCount> SubFilter { get; set; }

        /// <summary>
        /// <c>true</c> when the data is for a RIBF, else <c>false</c>.
        /// </summary>
        [DataMember(Order = 8)]
        public bool IsReverse { get; set; }

    }
}
