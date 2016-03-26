

namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using HashAlgorithms;

    /// <summary>
    /// An invertible reverse Bloom filter backed up by a collection of smaller Bloom filters
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <typeparam name="TId">The type of the entity identifier</typeparam>
    /// <typeparam name="TCount">The type of the occurence count in the Bloom filter.</typeparam>
    /// <remarks>Combines multiple Bloom filters, but there is little to no advantage: size is about the same, eror rate is higher.</remarks>
    public class InvertibleReverseSplitBloomFilter<TEntity, TId,TCount> : 
        IInvertibleBloomFilter<TEntity, TId, TCount> 
        where TCount : struct
        where TId : struct
    {
        #region Fields
        private readonly IMurmurHash _murmurHash = new Murmur3();
        private readonly IBloomFilterConfiguration<TEntity, TId, int, TCount> _configuration;
        private Lazy<IInvertibleBloomFilter<KeyValuePair<TId,int>, TId,  TCount>>[] _reverseBloomFilters;
        private uint _hashFunctionCount;
        private long _maxSubFilters;
        private long _blockSize;
        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new Bloom filter using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="maxSubFilters">The maximum number of sub-filters to be used.</param>
        public InvertibleReverseSplitBloomFilter(
            IBloomFilterConfiguration<TEntity,TId, int, TCount> bloomFilterConfiguration, 
            long maxSubFilters = 50)
        {
            _configuration = bloomFilterConfiguration;
            _maxSubFilters = maxSubFilters;
        }
        #endregion

        #region Implementation of IInvertibleBloomFilter{TEntity,TId,TCount}

        /// <summary>
        /// Add an entity to the filter.
        /// </summary>
        /// <param name="item"></param>
        public void Add(TEntity item)
        {
            var id = _configuration.GetId(item);
            var entityHash = _configuration.EntityHash(item);
            _reverseBloomFilters[GetIdx(id, entityHash)].Value.Add(new KeyValuePair<TId, int>(id, entityHash));
        }

        /// <summary>
        /// Determine if an entity is contained in the filter.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(TEntity item)
        {
            var id = _configuration.GetId(item);
            var entityHash = _configuration.EntityHash(item);
            return _reverseBloomFilters[GetIdx(id, entityHash)].Value.Contains(new KeyValuePair<TId, int>(id, entityHash));
        }

        /// <summary>
        /// Determine if a given entity identifier is in the filter.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Not supported.</exception>
        public bool ContainsKey(TId key)
        {
            throw new NotSupportedException("ContainsKey is not supported by the invertible reverse Bloom filter.");
        }       

        /// <summary>
        /// Extract the Bloom filter data
        /// </summary>
        /// <returns></returns>
        public InvertibleBloomFilterData<TId, int, TCount> Extract()
        {
            var res = new InvertibleBloomFilterData<TId, int, TCount>
            {
               SubFilterCount = _reverseBloomFilters.Length,
               BlockSize = _blockSize,
               HashFunctionCount = _hashFunctionCount,
               IsReverse = true,
                SubFilters = _reverseBloomFilters.Where(r => r.IsValueCreated).Select(r => r.Value.Extract()).ToArray(),
                SubFilterIndexes = _reverseBloomFilters
                    .Select((f, i) => new {IsCreated = f.IsValueCreated, Index = i})
                    .Where(f => f.IsCreated)
                    .Select(f => f.Index)
                    .ToArray()
            };
            if (res.SubFilterIndexes.Length == res.SubFilters.Length)
            {
                res.SubFilterIndexes = null;
            }
            return res;
        }

        /// <summary>
        /// Initialize the filter.
        /// </summary>
        /// <param name="capacity"></param>
        public void Initialize(long capacity)
        {            
            Initialize(capacity, _configuration.BestErrorRate(capacity));
        }

        /// <summary>
        /// Initialize the filter
        /// </summary>
        /// <param name="capacity">The capacity</param>
        /// <param name="errorRate">The error rate</param>
        public void Initialize(long capacity, float errorRate)
        {
            _blockSize = _maxSubFilters;
            var capacityPerSubFilter = (long)Math.Floor(1.0D * capacity / _blockSize);
            if (capacityPerSubFilter < 10)
            {
                _blockSize = (int) Math.Floor(_blockSize/2.0D);
                while (_blockSize > 0)
                {
                    capacityPerSubFilter = (long) Math.Floor(1.0D*capacity/_blockSize);
                    if (capacityPerSubFilter > 10)
                    {
                        break;
                    }
                    _blockSize = (int) Math.Floor(_blockSize/2.0D);
                }
                if (capacityPerSubFilter < 10 || _blockSize < 1)
                {
                    _blockSize = 1;
                    capacityPerSubFilter = capacity;
                }
            }
            _hashFunctionCount = _configuration.BestHashFunctionCount(capacityPerSubFilter, errorRate);
            Initialize(
                _blockSize,
                capacityPerSubFilter,
                _configuration.BestCompressedSize(capacityPerSubFilter, errorRate),
                _hashFunctionCount
                );
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="capacity">The capacity</param>
        /// <param name="m">The size of a single sub filter.</param>
        /// <param name="k">The number of hash functions</param>
        /// <remarks>Will use the maximum number of sub filters set.</remarks>
        public void Initialize(long capacity, long m, uint k)
        {
            _hashFunctionCount = k;
            _blockSize = Math.Max(1,_maxSubFilters);
            var internalCapacity = (long)Math.Max(1, Math.Floor(1.0D *capacity / _maxSubFilters));           
            Initialize(_maxSubFilters, internalCapacity, m, _hashFunctionCount);
        }

        private void Initialize(long subFilterCount, long capacity, long m, uint k)
        {
            _reverseBloomFilters = LongEnumerable.Range(0L, subFilterCount)
                .Select(
                    i =>
                        new Lazy<IInvertibleBloomFilter<KeyValuePair<TId, int>, TId, TCount>>(
                            () =>
                            {
                                var res = new InvertibleReverseBloomFilter<KeyValuePair<TId, int>, TId, TCount>(
                                    _configuration.SubFilterConfiguration);
                                res.Initialize(capacity, m, k);
                                return res;
                            }))
                .ToArray();
        }

        /// <summary>
        /// Rehydrate the given <paramref name="data">Bloom filter data</paramref>.
        /// </summary>
        /// <param name="data"></param>
        public void Rehydrate(IInvertibleBloomFilterData<TId, int, TCount> data)
        {           
            if ((data?.SubFilters?.Length??0)== 0) return;
            _maxSubFilters = data.SubFilterCount;
            _blockSize = data.BlockSize;
            _hashFunctionCount = data.HashFunctionCount;
            _reverseBloomFilters = LongEnumerable.Range(0L, data.SubFilterCount)
                .Select(
                    i =>
                        new Lazy<IInvertibleBloomFilter<KeyValuePair<TId, int>, TId, TCount>>(
                            () =>
                            {
                                var res = new InvertibleReverseBloomFilter<KeyValuePair<TId, int>, TId, TCount>(
                                    _configuration.SubFilterConfiguration);
                                res.Rehydrate(data.GetSubFilter(i));
                                return res;
                            }))
                .ToArray();
        }

        /// <summary>
        /// Remove an item from the Bloom filter
        /// </summary>
        /// <param name="item"></param>
        public void Remove(TEntity item)
        {
            var id = _configuration.GetId(item);
            var entityHash = _configuration.EntityHash(item);
            _reverseBloomFilters[GetIdx(id, entityHash)].Value.Remove(new KeyValuePair<TId, int>(id, entityHash));
        }

        /// <summary>
        /// Remove a key.
        /// </summary>
        /// <param name="key"></param>
        public void RemoveKey(TId key)
        {
            throw new NotSupportedException("RemoveKey is not supported by an invertible reverse Bloom filter.");
        }

        /// <summary>
        /// Subtract and decode
        /// </summary>
        /// <param name="filter">The filter to subtract.</param>
        /// <param name="listA">Entity identifier in this filter, but not in <paramref name="filter"/>.</param>
        /// <param name="listB">Entity identifiers in <paramref name="filter"/>, but not in this filter</param>
        /// <param name="modifiedEntities">Entity identifiers in both filters, but with a different value.</param>
        /// <returns><c>true</c> when decode succeeded, else <c>false</c>.</returns>
        public bool SubtractAndDecode(
            IInvertibleBloomFilter<TEntity, TId, TCount> filter, 
            HashSet<TId> listA, 
            HashSet<TId> listB, 
            HashSet<TId> modifiedEntities)
        {
            return SubtractAndDecode(listA, listB, modifiedEntities, filter.Extract());
        }

        /// <summary>
        /// Subtract and decode
        /// </summary>
        /// <param name="filter">The filter to subtract.</param>
        /// <param name="listA">Entity identifier in this filter, but not in <paramref name="filter"/>.</param>
        /// <param name="listB">Entity identifiers in <paramref name="filter"/>, but not in this filter</param>
        /// <param name="modifiedEntities">Entity identifiers in both filters, but with a different value.</param>
        /// <returns><c>true</c> when decode succeeded, else <c>false</c>.</returns>
        public bool SubtractAndDecode(
            HashSet<TId> listA, 
            HashSet<TId> listB, 
            HashSet<TId> modifiedEntities,  
            IInvertibleBloomFilterData<TId, int, TCount> filter)
        {
            return Extract().SubtractAndDecode(filter, _configuration, listA, listB, modifiedEntities);
        }
        #endregion

        #region Methods

        private long GetIdx(TId key, int hash)
        {
            if (_reverseBloomFilters == null || _reverseBloomFilters.Length <=  1) return 0;
             var idxHash =
                BitConverter.ToInt32(
                    _murmurHash.Hash(BitConverter.GetBytes(hash), unchecked((uint) _configuration.IdHash(key))), 0);                      
            var pos = Math.Abs(idxHash%_reverseBloomFilters.Length);
            return pos;
        }
        #endregion
    }
}