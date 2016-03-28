
namespace TBag.BloomFilters
{
    using Configurations;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// An invertible Bloom filter supports removal and additions.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <typeparam name="TId">The type of the entity identifier</typeparam>
    /// <typeparam name="TCount">The type of the occurence count in the Bloom filter.</typeparam>
    /// <remarks>Contains both a regular invertible Bloom filter and a reverse invertible Bloom filter.</remarks>
    public class InvertibleHybridBloomFilter<TEntity, TId,TCount> : 
        InvertibleBloomFilter<TEntity, TId,TCount>
        where TCount : struct
        where TId : struct
    {
        #region Fields
        private readonly IInvertibleBloomFilter<KeyValuePair<TId,int>, TId,  TCount> _reverseBloomFilter;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Bloom filter using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
         public InvertibleHybridBloomFilter(
            IBloomFilterConfiguration<TEntity,TId, int, TCount> bloomFilterConfiguration) : base(bloomFilterConfiguration)
        {
            _reverseBloomFilter = new InvertibleReverseBloomFilter<KeyValuePair<TId, int>, TId, TCount>(
                bloomFilterConfiguration.ConvertToKeyValueHash());
           ValidateConfiguration = false;
        }
        #endregion

        #region Implementation of Bloom Filter public contract
        /// <summary>
        /// Add an entity
        /// </summary>
        /// <param name="item">The entity to add</param>
        public override void Add(TEntity item)
        {
            var entityHash = Configuration.EntityHash(item);
            var id = Configuration.GetId(item);
             Add(id, Configuration.IdHash(id));
            _reverseBloomFilter.Add(new KeyValuePair<TId, int>(id, entityHash));
        }

        /// <summary>
        /// Remove an entity
        /// </summary>
        /// <param name="item">The entity to remove</param>
        public override void Remove(TEntity item)
        {
            var entityHash = Configuration.EntityHash(item);
            var id = Configuration.GetId(item);
            var idHash = Configuration.IdHash(id);
            RemoveKey(id, idHash);
            _reverseBloomFilter.Remove(new KeyValuePair<TId, int>(id, entityHash));
        }

        /// <summary>
        /// Determine if the Bloom filter contains the item
        /// </summary>
        /// <param name="item">The item to check for</param>
        /// <returns></returns>
        public override bool Contains(TEntity item)
        {
            var id = Configuration.GetId(item);
            return ContainsKey(id, Configuration.IdHash(id));
        }

        /// <summary>
        /// Remove the given key
        /// </summary>
        /// <param name="key">Key to remove</param>
        /// <exception cref="NotSupportedException">Not supported</exception>
        public override void RemoveKey(TId key)
        {
            throw new NotSupportedException("RemoveKey is not supported for an invertible hybrid Bloom filter. Please use a regular IBF.");
        }

        /// <summary>
        /// Initialize the Bloom filter
        /// </summary>
        /// <param name="capacity">The capacity (number of elements to store in the Bloom filter)</param>
        /// <param name="m">The size of the Bloom filter per hash function</param>
        /// <param name="k">The number of the hash function</param>
        public override void Initialize(long capacity, long m, uint k)
        {
            base.Initialize(capacity, m, k);
            _reverseBloomFilter.Initialize(capacity, m, k);
            Data.SubFilters =new [] { _reverseBloomFilter.Extract() };
            Data.SubFilterCount = 1;
        }

        /// <summary>
        /// Restore the data of the Bloom filter
        /// </summary>
        /// <param name="data">The data to restore</param>
        public override void Rehydrate(IInvertibleBloomFilterData<TId, int, TCount> data)
        {
            if (data?.SubFilters == null) return;
            if (data.SubFilters.Length != 1)
                throw new ArgumentException("Data and value filter data are required for a hybrid estimator.", nameof(data));
            base.Rehydrate(data);
            _reverseBloomFilter.Rehydrate(data.SubFilters[0]);
        }
        #endregion
    }
}