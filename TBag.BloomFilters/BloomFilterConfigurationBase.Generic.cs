namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for a Bloom filter configuration
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The identifier type</typeparam>
    /// <typeparam name="THash">The hash value type</typeparam>
    /// <typeparam name="TCount">The type of the occurence counter</typeparam>
    public abstract class BloomFilterConfigurationBase<TEntity, TId, THash, TCount> :
        BloomFilterIdConfigurationBase<TEntity, TId, THash>,
        IBloomFilterConfiguration<TEntity, TId, THash, TCount>
        where THash : struct
        where TCount : struct
        where TId : struct
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="createFilter"></param>
        protected BloomFilterConfigurationBase(bool createFilter = true)
        {
            if (createFilter)
            {
                ValueFilterConfiguration = this.ConvertToKeyValueHash();
            }
        }

        public virtual ICountConfiguration<TCount> CountConfiguration
        {
            get; set;
        }

        public IInvertibleBloomFilterDataFactory DataFactory { get; } = new InvertibleBloomFilterDataFactory();

        public virtual Func<TEntity, THash> EntityHash
        {
            get; set;
        }

        public virtual EqualityComparer<THash> HashEqualityComparer
        {
            get; set;
        }

        public virtual Func<THash> HashIdentity
        {
            get; set;
        }

        public virtual Func<THash, THash, THash> HashXor
        {
            get; set;
        }

        public virtual Func<IInvertibleBloomFilterData<TId, THash, TCount>, long, bool> IsPure
        {
            get; set;
        }

        public virtual IBloomFilterConfiguration<KeyValuePair<TId, THash>, TId, THash, TCount> ValueFilterConfiguration
        {
            get; set;
        }

        /// <summary>
        /// Determine if the configuration supports the given capacity and set size.
        /// </summary>
        /// <param name="capacity">Capacity for the Bloom filter</param>
        /// <param name="size">The actual set size.</param>
        /// <returns></returns>
        public abstract bool Supports(long capacity, long size);
    }
}

  
