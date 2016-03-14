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
        [DataMember(Order=1)]
        public long BlockSize { get; set; }

        [DataMember(Order = 2)]
        public uint HashFunctionCount { get; set; }

        [DataMember(Order = 3)]
        public TId[] IdSums { get; set; }

        [DataMember(Order = 4)]
        public TCount[] Counts { get; set; }

        [DataMember(Order = 5)]
        public TEntityHash[] HashSums { get; set; }   
        
        [DataMember(Order = 6)]
        public InvertibleBloomFilterData<TEntityHash, TId, TCount>  ValueFilter { get; set; }

        [DataMember(Order = 7)]
        public bool IsReverse { get; set; }
    }
}
