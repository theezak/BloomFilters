namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for a Bloom filter configuration
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The identifier type</typeparam>
    /// <typeparam name="TEntityHash"></typeparam>
    /// <typeparam name="THash">The hash value type</typeparam>
    /// <typeparam name="TCount">The occurence count type.</typeparam>
    public abstract class BloomFilterConfigurationBase<TEntity, TId, TEntityHash, THash, TCount> :
        BloomFilterIdConfigurationBase<TId, THash>,
        IBloomFilterConfiguration<TEntity,TId, TEntityHash, THash,  TCount>
          where TEntityHash : struct
        where THash : struct
        where TCount : struct
        where TId : struct
    {
        
        /// <summary>
        /// Get the hash for the values of an entity.
        /// </summary>
        protected virtual Func<TEntity, THash> GetEntityHash { get; set; }

        /// <summary>
        /// Get the identifier for an entity.
        /// </summary>
        public virtual Func<TEntity, TId> GetId { get; set; }

        /// <summary>
        /// Function to determine if an entity hash equals the identity value (for example: zero for numbers).
        /// </summary>
        public virtual Func<TEntityHash, bool> IsEntityHashIdentity { get; set; }

        /// <summary>
        /// Calculate the XOR of two entity hasehs.
        /// </summary>
        public virtual Func<TEntityHash, TEntityHash, TEntityHash> EntityHashXor { get; set; }

        /// <summary>
        /// The unity for the count type.
        /// </summary>
        public virtual Func<TCount> CountUnity { get; set; }

        /// <summary>
        /// Determine if the count is pure.
        /// </summary>
        public virtual Func<TCount, bool> IsPureCount { get; set; }

        /// <summary>
        /// Decrease the count
        /// </summary>
        public virtual Func<TCount,TCount> CountDecrease { get; set; }

        /// <summary>
        /// Count identity.
        /// </summary>
        public virtual Func<TCount> CountIdentity { get; set; }

        /// <summary>
        /// Subtract two counts.
        /// </summary>
        public virtual Func<TCount, TCount, TCount> CountSubtract { get; set; }

        /// <summary>
        /// The entity hashes.
        /// </summary>
        public virtual Func<TEntity, uint, IEnumerable<TEntityHash>> EntityHashes { get; set; }

        /// <summary>
        /// Increase the count.
        /// </summary>
        public virtual Func<TCount,  TCount> CountIncrease { get; set; }

        public IBloomFilterConfiguration<TEntity, TEntityHash, TId, THash, TCount> ValueFilterConfiguration { get; protected set; }

        /// <summary>
        /// Determine if the configuration supports the given capacity and set size.
        /// </summary>
        /// <param name="capacity">Capacity for the Bloom filter</param>
        /// <param name="size">The actual set size.</param>
        /// <returns></returns>
        public abstract bool Supports(ulong capacity, ulong size);
    }
}

  
