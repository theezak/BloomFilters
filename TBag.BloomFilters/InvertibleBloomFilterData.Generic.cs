namespace TBag.BloomFilters
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Implementation of <see cref="IInvertibleBloomFilterData{TId}"/>
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    [DataContract, Serializable]
    public class InvertibleBloomFilterData<TId, TCount> : IInvertibleBloomFilterData<TId, TCount>
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
        public TCount[] Counts { get; set; }
    }
}
