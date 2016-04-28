using System.Linq;

namespace TBag.BloomFilters.Invertible
{
    using Configurations;
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
        /// The item count.
        /// </summary>
        public virtual long ItemCount => Data?.ItemCount ?? 0L;

        /// <summary>
        /// The capacity.
        /// </summary>
        public virtual long Capacity => Data?.Capacity ?? 0L;

        /// <summary>
        /// The error rate
        /// </summary>
        public virtual float ErrorRate => Data?.ErrorRate ?? 0.0000001F;

        /// <summary>
        /// The hash function count
        /// </summary>
        public virtual uint HashFunctionCount => Data?.HashFunctionCount ?? 0;
      
        /// <summary>
        /// The configuration for the Bloom filter.
        /// </summary>
        public IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> Configuration { get; }

        /// <summary>
        /// The Bloom filter data.
        /// </summary>
        protected InvertibleBloomFilterData<TId, int, TCount> Data { get; private set; }

        /// <summary>
        /// The block size.
        /// </summary>
        public virtual long BlockSize => Data?.BlockSize ?? 0L;

         #endregion

        #region Constructors

        /// <summary>
        /// Creates a new Bloom filter 
        /// </summary>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="validateConfiguration">When <c>true</c> the configuration is validated on the first operation, else <c>false</c>.</param>
        public InvertibleBloomFilter(
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> bloomFilterConfiguration,
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
        /// <param name="foldFactor"></param>
        public void Initialize(long capacity, int foldFactor = 0)
        {
            Initialize(capacity, Configuration.BestErrorRate(capacity), foldFactor);
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <param name="errorRate">The desired error rate (between 0 and 1).</param>
        /// <param name="foldFactor"></param>
        public void Initialize(long capacity, float errorRate, int foldFactor = 0)
        {
            Initialize(
                capacity,
                Configuration.BestCompressedSize(capacity, errorRate, foldFactor),
                Configuration.BestHashFunctionCount(capacity, errorRate));
            Data.ErrorRate = errorRate;
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
            if (!Configuration.Supports(capacity, m))
            {
                throw new ArgumentOutOfRangeException(
                    $"The size {m} of the Bloom filter is not large enough to hold {capacity} items.");
            }
            Data = Configuration.DataFactory.Create(Configuration, capacity, m, k);
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
        /// Add the Bloom filter
        /// </summary>
        /// <param name="bloomFilter">Bloom filter to add</param>
        /// <exception cref="ArgumentException">Bloom filter is not compatible</exception>
        public void Add(IInvertibleBloomFilter<TEntity, TId, TCount> bloomFilter)
        {
            if (bloomFilter == null) return;
            var result = Extract().Add(Configuration, bloomFilter.Extract());
            if (result == null)
            {
                throw new ArgumentException("An incompatible Bloom filter cannot be added.", nameof(bloomFilter));
            }
            Rehydrate(result);
        }

        /// <summary>
        /// Add the Bloom filter data
        /// </summary>
        /// <param name="bloomFilterData">Bloom filter data to add</param>
        /// <exception cref="ArgumentException">Bloom filter data is not compatible</exception>
        public void Add(IInvertibleBloomFilterData<TId, int, TCount> bloomFilterData)
        {
            if (bloomFilterData == null) return;
            bloomFilterData.SyncCompressionProviders(Configuration);
            var result = Extract().Add(Configuration, bloomFilterData);
            if (result == null)
            {
                throw new ArgumentException("An incompatible Bloom filter cannot be added.", nameof(bloomFilterData));
            }
            Rehydrate(result);
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
            if (data== null) return;
            if (!data.IsValid())
                throw new ArgumentException(
                    "Invertible Bloom filter data is invalid.",
                    nameof(data));
            Data = data.ConvertToBloomFilterData(Configuration);
            ValidateData();
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
        public bool? SubtractAndDecode(IInvertibleBloomFilter<TEntity, TId, TCount> filter,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities)
        {
            return SubtractAndDecode(listA, listB, modifiedEntities, filter.Extract());
        }

        /// <summary>
        /// Intersect a Bloom filter with the current Bloom filter.
        /// </summary>
        /// <param name="bloomFilter"></param>
        public void Intersect(IInvertibleBloomFilter<TEntity,TId,TCount> bloomFilter)
        {
            var result = Extract().Intersect(Configuration, bloomFilter.Extract());
            if (result == null)
            {
                throw new ArgumentException("An incompatible Bloom filter cannot be intersected.", nameof(bloomFilter));
            }
            Rehydrate(result);
        }

        /// <summary>
        /// Intersect a Bloom filter with the current Bloom filter.
        /// </summary>
        /// <param name="otherFilterData"></param>
        public void Intersect(IInvertibleBloomFilterData<TId, int, TCount> otherFilterData)
        {
            var result = Extract().Intersect(Configuration, otherFilterData);
            if (result == null)
            {
                throw new ArgumentException("An incompatible Bloom filter cannot be intersected.", nameof(otherFilterData));
            }
            Rehydrate(result);
        }

        /// <summary>
        /// Subtract and then decode.
        /// </summary>
        /// <param name="filterData">Bloom filter to subtract</param>
        /// <param name="listA">Items in this filter, but not in <paramref name="filterData"/></param>
        /// <param name="listB">Items not in this filter, but in <paramref name="filterData"/></param>
        /// <param name="modifiedEntities">Entities in both filters, but with a different value</param>
        /// <returns><c>true</c> when the decode was successful, otherwise <c>false</c></returns>
        public virtual bool? SubtractAndDecode(
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities,
            IInvertibleBloomFilterData<TId, int, TCount> filterData)
        {
            if (!ValidateData()) return null;
            filterData?.SyncCompressionProviders(Configuration);
            return Data.SubtractAndDecode(filterData, Configuration, listA, listB, modifiedEntities);
        }

        /// <summary>
        /// Fold the Bloom filter by the given factor.
        /// </summary>
        /// <param name="factor"></param>
        /// <param name="destructive">When <c>true</c> the Bloom filter instance is replaced by the folded Bloom filter, else <c>false</c>.</param>
        /// <exception cref="ArgumentException">The fold factor is invalid.</exception>
        public virtual IInvertibleBloomFilter<TEntity,TId,TCount> Fold(uint factor, bool destructive = false)
        {
            var res = Extract().Fold(Configuration, factor);
            if (destructive)
            {
                Rehydrate(res);
                return this;
            }
            return CreateNewInstance(res);
        }

        /// <summary>
        /// Compress the Bloom filter.
        /// </summary>
        /// <param name="inPlace"></param>
        /// <returns></returns>
        public virtual IInvertibleBloomFilter<TEntity, TId, TCount> Compress(bool inPlace = false)
        {
            var res = Extract().Compress(Configuration);
            if (inPlace)
            {
                Rehydrate(res);
                return this;
            }
            return CreateNewInstance(res);
        }

        #endregion

        #region Methods
        /// <summary>
        /// Create a new Bloom filter with the given data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual IInvertibleBloomFilter<TEntity, TId, TCount> CreateNewInstance(IInvertibleBloomFilterData<TId, int, TCount> data)
        {
            var bloomFilter = new InvertibleBloomFilter<TEntity, TId, TCount>(Configuration);
            bloomFilter.Rehydrate(data);
            return bloomFilter;
        }

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
            Data.ItemCount++;
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
            var countIdentity = Configuration.CountConfiguration.Identity;
            var countUnity = Configuration.CountConfiguration.Unity;
            var countConfiguration = Configuration.CountConfiguration;
            return Configuration
                .Probe(Data, hash)
                .All(position =>
                {
                    var count = Data.Counts[position];
                    if (Configuration.IsPure(Data, position) &&
                        (!Configuration.IdEqualityComparer.Equals(Data.IdSumProvider[position], key) ||
                         !Configuration.HashEqualityComparer.Equals(Data.HashSumProvider[position], hash)))
                    {
                        return false;
                    }
                    var countComparedToIdentity = countConfiguration.Comparer.Compare(count, countIdentity);
                    if (countComparedToIdentity == 0)
                    {
                        return false;
                    }
                    if (countComparedToIdentity > 0 &&
                        countConfiguration.IsPure(countConfiguration.Subtract(count, countUnity)))
                    {
                        Data.Remove(Configuration, key, hash, position);
                        var pureAfterRemoval = Configuration.IsPure(Data, position);
                        Data.Add(Configuration, key, hash, position);
                        if (!pureAfterRemoval)
                        {
                            return false;
                        }
                    }
                    return true;
                });
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
            Data.ItemCount--;
        }

        /// <summary>
        /// Validate the data.
        /// </summary>
        protected virtual bool ValidateData()
        {
            if (Data==null)
            {
                throw new InvalidOperationException("The invertible Bloom filter was not initialized or rehydrated.");
            }
            if (Data.IsReverse)
            {
                throw new InvalidOperationException("An invertible Bloom filter does not accept reverse Bloom filter data. Please use a reverse Bloom filter.");
            }
            return true;
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