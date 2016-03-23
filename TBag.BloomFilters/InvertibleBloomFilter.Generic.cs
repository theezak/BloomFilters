namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
 
    /// <summary>
    /// An invertible Bloom filter supports removal and additions.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <typeparam name="TId">The type of entity identifier</typeparam>
    /// <typeparam name="TCount">The type of the counter</typeparam>
    /// <remarks>Only determines set differences. It does not handle key/value pairs.</remarks>
    public class InvertibleBloomFilter<TEntity, TId, TCount> :
        IInvertibleBloomFilter<TEntity, TId, TCount>
        where TCount : struct
        where TId : struct
    {
          #region Properties
        /// <summary>
        /// When <c>true</c> the configuration will be validated, else <c>false</c>.
        /// </summary>
        protected bool ValidateConfiguration { get; set; }
      
        /// <summary>
        /// The configuration for the Bloom filter.
        /// </summary>
        protected IBloomFilterConfiguration<TEntity, TId, int, TCount> Configuration { get; }

        /// <summary>
        /// The Bloom filter data.
        /// </summary>
        protected InvertibleBloomFilterData<TId, int, TCount> Data { get; private set; }
        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new Bloom filter 
        /// </summary>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="validateConfiguration">When <c>true</c> the configuration is validated on the first operation, else <c>false</c>.</param>
        public InvertibleBloomFilter(
            IBloomFilterConfiguration<TEntity, TId, int, TCount> bloomFilterConfiguration,
            bool validateConfiguration = true)
        {
            Configuration = bloomFilterConfiguration;
            ValidateConfiguration = validateConfiguration;
        }

        #endregion

        #region Implementation of Bloom Filter public contract
        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="capacity">The capacity</param>
        public void Initialize(long capacity)
        {
            Initialize(capacity, Configuration.BestErrorRate(capacity));
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <param name="errorRate">The desired error rate (between 0 and 1).</param>
        public void Initialize(long capacity, float errorRate)
        {
            Initialize(
                capacity,
                Configuration.BestCompressedSize(capacity, errorRate),
                Configuration.BestHashFunctionCount(capacity, errorRate));
        }

        /// <summary>
        /// Initialize the Bloom filter
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <param name="m">Size per hash function</param>
        /// <param name="k">Number of hash functions.</param>
        public virtual void Initialize(long capacity, long m, uint k)
        {
            // validate the params are in range
            if (!Configuration.Supports(capacity, m * k))
            {
                throw new ArgumentOutOfRangeException(
                    $"The size {m * k} of the Bloom filter is not large enough to hold {capacity} items.");
            }
            Data = Configuration.DataFactory.Create<TId, int, TCount>(m, k);            
        }

        /// <summary>
        /// Add an item to the Bloom filter.
        /// </summary>
        /// <param name="item">The entity to add.</param>
        public virtual void Add(TEntity item)
        {
            ValidateData();
            Add(Configuration.GetId(item), Configuration.EntityHash(item));
        }

        /// <summary>
        /// Extract the Bloom filter in a serializable format.
        /// </summary>
        /// <returns></returns>
        public virtual InvertibleBloomFilterData<TId, int, TCount> Extract()
        {
            return Data;
        }

        /// <summary>
        /// Set the data for this Bloom filter.
        /// </summary>
        /// <param name="data">The data to restore</param>
        public virtual void Rehydrate(IInvertibleBloomFilterData<TId, int, TCount> data)
        {
            if (data == null) return;
            if (data.IsReverse)
                throw new ArgumentException("Invertible Bloom filter does not accept reverse data. Please use an invertible reverse Bloom filter.", nameof(data));
            if (!data.IsValid())
                throw new ArgumentException(
                    "Invertible Bloom filter data is invalid.",
                    nameof(data));
            Data = data.ConvertToBloomFilterData(Configuration);
        }

        /// <summary>
        /// Remove the given item from the Bloom filter.
        /// </summary>
        /// <param name="item">The entity to remove</param>
        public virtual void Remove(TEntity item)
        {
            RemoveKey(Configuration.GetId(item), Configuration.EntityHash(item));
        }

        /// <summary>
        /// Remove the given item from the Bloom filter.
        /// </summary>
        /// <param name="key">Key of the item to remove</param>
        public virtual void RemoveKey(TId key)
        {
            ValidateData();
            //using that IdHash equals EntityHash for a regular IBF
            RemoveKey(key, Configuration.IdHash(key));
        }

        /// <summary>
        /// Checks for the existance of the item in the filter for a given probability.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns></returns>
        /// <remarks>Contains is purely based upon the identifier. An idea is however to utilize the empty hash array for an id hash to double check deletions.</remarks>
        public virtual bool Contains(TEntity item)
        {
            return ContainsKey(Configuration.GetId(item), Configuration.EntityHash(item));
        }

        /// <summary>
        /// Determine if the Bloom filter contains the given key.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns></returns>
        /// <remarks>Will not work unless EntityHash is the HashIdentity.</remarks>
        public virtual bool ContainsKey(TId key)
        {
            ValidateData();
            //using that IdHash equals EntityHash for a regular IBF
            return ContainsKey(key, Configuration.IdHash(key));
        }

        /// <summary>
        /// Subtract and then decode.
        /// </summary>
        /// <param name="filter">Bloom filter to subtract</param>
        /// <param name="listA">Items in this filter, but not in <paramref name="filter"/></param>
        /// <param name="listB">Items not in this filter, but in <paramref name="filter"/></param>
        /// <param name="modifiedEntities">Entities in both filters, but with a different value</param>
        /// <returns><c>true</c> when the decode was successful, otherwise <c>false</c></returns>
        public bool SubtractAndDecode(IInvertibleBloomFilter<TEntity, TId, TCount> filter,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities)
        {
            return SubtractAndDecode(filter.Extract(), listA, listB, modifiedEntities);
        }

        /// <summary>
        /// Subtract and then decode.
        /// </summary>
        /// <param name="filter">Bloom filter to subtract</param>
        /// <param name="listA">Items in this filter, but not in <paramref name="filter"/></param>
        /// <param name="listB">Items not in this filter, but in <paramref name="filter"/></param>
        /// <param name="modifiedEntities">Entities in both filters, but with a different value</param>
        /// <returns><c>true</c> when the decode was successful, otherwise <c>false</c></returns>
        public virtual bool SubtractAndDecode(IInvertibleBloomFilterData<TId, int, TCount> filter,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities)
        {
            ValidateData();
            return Data.SubtractAndDecode(filter, Configuration, listA, listB, modifiedEntities);
        }

        #endregion

       
        #region Methods

        /// <summary>
        /// Add the identifier and hash.
        /// </summary>
        /// <param name="key">The key to add</param>
         /// <param name="entityHash">The entity hash</param>
        protected virtual void Add(TId key, int entityHash)
        {
            if (ValidateConfiguration)
            {
                IsValidConfiguration(Configuration.IdHash(key), entityHash);
            }
            foreach (var position in Configuration.Probe(Data, entityHash))
            {
                Data.Add(Configuration, key, entityHash, position);
            }
        }

        /// <summary>
        /// Given the key and probe hash, determine if the filter contains the key.
        /// </summary>
        /// <param name="key">The key to add</param>
        /// <param name="hash">The entity hash</param>
        /// <returns></returns>
       protected virtual bool ContainsKey(TId key, int hash)
        {
            if (ValidateConfiguration)
            {
                IsValidConfiguration(Configuration.IdHash(key), hash);
            }
            var countIdentity = Configuration.CountConfiguration.CountIdentity();
            var idIdentity = Configuration.IdIdentity();
            var hashIdentity = Configuration.HashIdentity();
            foreach (var position in Configuration.Probe(Data,  hash))
            {
                if (Configuration.IsPure(Data, position) &&
                     (!Configuration.IdEqualityComparer.Equals(Data.IdSums[position], key) ||
                         !Configuration.HashEqualityComparer.Equals(Data.HashSums[position], hash)))
                {
                    return false;
                }
                if (Configuration.CountConfiguration.EqualityComparer.Equals(Data.Counts[position], countIdentity))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Remove the given key
        /// </summary>
        /// <param name="key">The key to remove</param>
        /// <param name="hash">The entity hash</param>
       protected virtual void RemoveKey(TId key, int hash)
        {
            if (ValidateConfiguration)
            {
                IsValidConfiguration(Configuration.IdHash(key), hash);
            }
            foreach (var position in Configuration.Probe(Data,hash))
            {
                Data.Remove(Configuration, key, hash, position);
            }
        }

        /// <summary>
        /// Validate the data.
        /// </summary>
        protected void ValidateData()
        {
            if (Data==null)
            {
                throw new InvalidOperationException("The invertible Bloom filter was not initialized or rehydrated.");
            }            
        }

        /// <summary>
        /// Determine if the configuration is valid.
        /// </summary>
        /// <param name="idHash"></param>
        /// <param name="entityHash"></param>
        /// <remarks>For regular IBFs the entity hash and identifier hash have to be equal.</remarks>
        protected virtual void IsValidConfiguration(int idHash, int entityHash)
        {
            if (idHash != entityHash)
                throw new InvalidOperationException("The configuration of the IBF does not satisfy that the IdHash and EntityHash are equal. For key-value pairs, please use a reverse IBF or hybrid IBF.");
            ValidateConfiguration = false;
        }

        #endregion
    }
}