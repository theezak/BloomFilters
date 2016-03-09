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
