namespace TBag.BloomFilters.Invertible.Configurations
{
    using BloomFilters.Configurations;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for the Bloom filter configuration for identifiers.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity</typeparam>
    /// <typeparam name="TId">Type of the entity identifier</typeparam>
    /// <typeparam name="THash">Type of the hash value</typeparam>
    public abstract class BloomFilterIdConfigurationBase<TEntity, TId, THash> : IBloomFilterSizeConfiguration
       where THash : struct
    {
        private static readonly double Log2 = Math.Log(2.0D);
        private static readonly double Pow2Log2 = Math.Pow(2, Math.Log(2.0D));
        /// <summary>
        /// Constructor
        /// </summary>
        protected BloomFilterIdConfigurationBase()
        {
            MinimumHashFunctionCount = 2;
        }

        /// <summary>
        /// Function to determine a sequence (of given length) for a given identifier.
        /// </summary>
        public virtual Func<THash, uint, IEnumerable<THash>> Hashes { get; set; }

        /// <summary>
        /// Identifier hash
        /// </summary>
        public virtual Func<TId, THash> IdHash { get; set; }

        /// <summary>
        /// Get the identifier for an entity.
        /// </summary>
        public virtual Func<TEntity, TId> GetId { get; set; }

        /// <summary>
        /// Equality comparer for <typeparamref name="TId"/>
        /// </summary>
        public virtual EqualityComparer<TId> IdEqualityComparer
        { get; set; }

        /// <summary>
        /// The identity value for <typeparamref name="TId"/> (for example 0 when the identifier is a number).
        /// </summary>
        public virtual TId IdIdentity { get; set; }

        /// <summary>
        /// Determine the XOR of two identifiers.
        /// </summary>
        public virtual Func<TId, TId, TId> IdXor { get; set; }

        /// <summary>
        /// The minimum number of hash functions used.
        /// </summary>
        public uint MinimumHashFunctionCount { get; set; }

        /// <summary>
        /// Function to determine the best number of hash functions.
        /// </summary>
        /// <param name="capacity">The capacity</param>
        /// <param name="errorRate">The desired error rate (between 0 and 1).</param>
        /// <returns></returns>
        public uint BestHashFunctionCount(long capacity, float errorRate)
        {
            //at least 2 hash functions.
            return Math.Max(
                MinimumHashFunctionCount,
                (uint)Math.Ceiling(Math.Abs(Log2 * (1.0D * BestSize(capacity, errorRate) / capacity))));
        }

        /// <summary>
        /// Determine the best, compressed, size for the Bloom filter.
        /// </summary>
        /// <param name="capacity">The capacity</param>
        /// <param name="errorRate">The desired error rate (between 0 and 1).</param>
        /// <param name="foldFactor">The fold factor.</param>
        /// <returns></returns>
        public virtual long BestCompressedSize(long capacity, float errorRate, int foldFactor = 0)
        {
            //compress the size of the Bloom filter, by ln2.
            //TODO: causes too many false positives? Alternative is return BestSize(capacity, errorRate);
            return (long)(BestSize(capacity, errorRate) * Log2);
        }

        /// <summary>
        /// Determine the best, uncompressed, size for the Bloom filter.
        /// </summary>
        /// <param name="capacity">The capacity</param>
        /// <param name="errorRate">The desired error rate (between 0 and 1).</param>
        /// <returns></returns>
        public virtual long BestSize(long capacity, float errorRate)
        {
            return (long)Math.Abs(capacity * Math.Log(errorRate) / Pow2Log2);
        }
      
        /// <summary>
        /// This determines an error rate assuming that at higher capacity a higher error rate is acceptable as a trade off for space. Provide your own error rate if this does not work for you.
        /// </summary>
        /// <param name="capacity">The capacity for the Bloom filter.</param>
        /// <returns>An error rate (between 0 and 1)</returns>
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
