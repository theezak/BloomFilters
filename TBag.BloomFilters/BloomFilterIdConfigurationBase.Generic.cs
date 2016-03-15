namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
  
    /// <summary>
    /// Base class for the Bloom filter configuration for identifiers.
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="THash"></typeparam>
    public abstract class BloomFilterIdConfigurationBase<TId, THash> : IBloomFilterSizeConfiguration
       where THash : struct
    {
        /// <summary>
        /// Function to determine a sequence (of given length) for a given identifier.
        /// </summary>
        public virtual Func<TId, uint, IEnumerable<THash>> IdHashes { get; set; }

        /// <summary>
        /// Determine the XOR of two identifiers.
        /// </summary>
        public virtual Func<TId, TId, TId> IdXor { get; set; }

        public uint BestHashFunctionCount(long capacity, float errorRate)
        {
            //at least 3 hash functions.
            return Math.Max(
                3,
                (uint)Math.Ceiling(Math.Abs(Math.Log(2.0D) * (1.0D * BestSize(capacity, errorRate) / capacity))));
        }

        public virtual long BestCompressedSize(long capacity, float errorRate)
        {
            //compress the size of the Bloom filter, by ln2.
            return (long)(BestSize(capacity, errorRate) * Math.Log(2.0D));
        }

        public virtual long BestSize(long capacity, float errorRate)
        {
            return (long)Math.Abs((capacity * Math.Log(errorRate)) / Math.Pow(2, Math.Log(2.0D)));
        }

        /// <summary>
        /// This determines an error rate assuming that at higher capacity a higher error rate is acceptable as a trade off for space. Provide your own error rate if this does not work for you.
        /// </summary>
        /// <param name="capacity"></param>
        /// <returns></returns>
        /// <remarks>Error rates above 50% are filtered out.</remarks>
        public virtual float BestErrorRate(long capacity)
        {
            //heuristic for determing an error rate: as capacity becomes larger, the accepted error rate increases.
            var errRate = Math.Min(0.5F, (float)(0.000001F * Math.Pow(2.0D, Math.Log(capacity))));
            //determine the best size based upon capacity and the error rate determined above, then calculate the error rate.
            return Math.Min(0.5F, (float)Math.Pow(0.5D, 1.0D * BestSize(capacity, errRate) / capacity));
            // return Math.Min(0.5F, (float)Math.Pow(0.6185D, BestM(capacity, errRate) / capacity));
            // http://www.cs.princeton.edu/courses/archive/spring02/cs493/lec7.pdf
        }
    }
}