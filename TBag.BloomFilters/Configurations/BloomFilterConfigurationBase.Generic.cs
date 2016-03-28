﻿namespace TBag.BloomFilters.Configurations
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
        private IBloomFilterConfiguration<KeyValuePair<TId, THash>, TId, THash, TCount> _subFilterConfiguration;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="createFilter">When <c>true</c> create the value Bloom filter, else <c>false</c></param>
        protected BloomFilterConfigurationBase(bool createFilter = true)
        {
            if (createFilter)
            {
                _subFilterConfiguration = this.ConvertToKeyValueHash();
            }
        }

        /// <summary>
        /// The count configuration.
        /// </summary>
        public virtual ICountConfiguration<TCount> CountConfiguration
        {
            get; set;
        }

        /// <summary>
        /// The data factory
        /// </summary>
        public IInvertibleBloomFilterDataFactory DataFactory { get; } = new InvertibleBloomFilterDataFactory();

        /// <summary>
        /// The entity hash
        /// </summary>
        /// <remarks>Used for generating positions to hash to. Either match the identifier hash, or a full entity hash (identifier and value).</remarks>
        public virtual Func<TEntity, THash> EntityHash
        {
            get; set;
        }

        /// <summary>
        /// Equality comparer for the hash.
        /// </summary>
        public virtual EqualityComparer<THash> HashEqualityComparer
        {
            get; set;
        }

        /// <summary>
        /// Identity value for the hash (for example 0 when the hash type is numeric).
        /// </summary>
        public virtual Func<THash> HashIdentity
        {
            get; set;
        }

        /// <summary>
        /// The XOR operator for the hash.
        /// </summary>
        public virtual Func<THash, THash, THash> HashXor
        {
            get; set;
        }

        /// <summary>
        /// Determine if a given position is pure.
        /// </summary>
        public virtual Func<IInvertibleBloomFilterData<TId, THash, TCount>, long, bool> IsPure
        {
            get; set;
        }

        /// <summary>
        /// The value filter configuration 
        /// </summary>
        /// <remarks>Only used for hybrid IBFs.</remarks>
        public virtual IBloomFilterConfiguration<KeyValuePair<TId, THash>, TId, THash, TCount> SubFilterConfiguration
        {
            get { return _subFilterConfiguration; }
            set { _subFilterConfiguration = value; }
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

  