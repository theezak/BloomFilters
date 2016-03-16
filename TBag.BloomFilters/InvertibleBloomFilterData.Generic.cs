namespace TBag.BloomFilters
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Implementation of <see cref="IInvertibleBloomFilterData{TId, TEntityHash, TCount}"/>
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TEntityHash"></typeparam>
    /// <typeparam name="TCount"></typeparam>
    [DataContract, Serializable]
    public class InvertibleBloomFilterData<TId, TEntityHash, TCount> : 
        IInvertibleBloomFilterData<TId, TEntityHash, TCount>
        where TCount : struct
        where TEntityHash : struct
        where TId : struct
    {
        /// <summary>
        /// The number of cells for a single hash function.
        /// </summary>
        [DataMember(Order=1)]
        public long BlockSize { get; set; }

        /// <summary>
        /// The number of hash functions
        /// </summary>
        [DataMember(Order = 2)]
        public uint HashFunctionCount { get; set; }

        /// <summary>
        /// An array of identifier (key) sums.
        /// </summary>
        [DataMember(Order = 3)]
        public TId[] IdSums { get; set; }

        /// <summary>
        /// An array of counts.
        /// </summary>
        [DataMember(Order = 4)]
        public TCount[] Counts { get; set; }

        /// <summary>
        /// An array of hash value sums.
        /// </summary>
        [DataMember(Order = 5)]
        public TEntityHash[] HashSums { get; set; }   
        
        /// <summary>
        /// The data for the reverse IBF
        /// </summary>
        /// <remarks>Only used by the hybrid IBF</remarks>
        [DataMember(Order = 6)]
        public InvertibleBloomFilterData<TEntityHash, TId, TCount>  ReverseFilter { get; set; }

        /// <summary>
        /// <c>true</c> when the data is for a RIBF, else <c>false</c>.
        /// </summary>
        [DataMember(Order = 7)]
        public bool IsReverse { get; set; }
    }
}
