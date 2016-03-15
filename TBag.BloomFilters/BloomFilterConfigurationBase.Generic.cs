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
        /// The identity value for <typeparamref name="TId"/> (for example 0 when the identifier is a number).
        /// </summary>
        public virtual Func<TId> IdIdentity { get; set; }

        /// <summary>
        /// The identity value for <typeparamref name="TEntityHash"/> (for example 0 when the identifier is a number).
        /// </summary>
        public virtual Func<TEntityHash> EntityHashIdentity { get; set; }

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

        /// <summary>
        /// The configuration of the reverse IBF 
        /// </summary>
        /// <remarks>Only utilized by the hybrid IBF</remarks>
        public IBloomFilterConfiguration<TEntity, TEntityHash, TId, THash, TCount> ValueFilterConfiguration { get; protected set; }

        /// <summary>
        /// Defines the pure operator for an IBF
        /// </summary>
        public virtual Func<IInvertibleBloomFilterData<TId, TEntityHash, TCount>, long, bool> IsPure { get; set; }

        /// <summary>
        /// Equality comparer for <typeparamref name="TEntityHash"/>.
        /// </summary>
        public virtual EqualityComparer<TEntityHash> EntityHashEqualityComparer
        { get; set; }

        /// <summary>
        /// Equality comparer for <typeparamref name="THash"/>
        /// </summary>
        public virtual EqualityComparer<THash> IdHashEqualityComparer
        { get; set; }

        /// <summary>
        /// Equality comparer for <typeparamref name="TId"/>
        /// </summary>
        public virtual EqualityComparer<TId> IdEqualityComparer
        { get; set; }

        /// <summary>
        /// Equality comparer for <typeparamref name="TCount"/>
        /// </summary>
        public virtual EqualityComparer<TCount> CountEqualityComparer
        { get; set; }
      
        /// <summary>
        /// Determine if the configuration supports the given capacity and set size.
        /// </summary>
        /// <param name="capacity">Capacity for the Bloom filter</param>
        /// <param name="size">The actual set size.</param>
        /// <returns></returns>
        public abstract bool Supports(ulong capacity, ulong size);
    }
}

  
