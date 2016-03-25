

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
    /// <remarks>based upon the notion that, first of all, a collection of smaller Bloom filters is more resilient to being undersized. Secondly, a collection will actually have a slightly smaller footprint than a single large Bloom filter. Thirdly, it iseasier to support parallellism</remarks>
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
        private long _blockSize;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Bloom filter using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
         public InvertibleReverseSplitBloomFilter(
            IBloomFilterConfiguration<TEntity,TId, int, TCount> bloomFilterConfiguration, long maxSubFilters = 0)
        {
            _configuration = bloomFilterConfiguration;
            _blockSize = maxSubFilters;
        }
        #endregion

        #region Implementation of IInvertibleBloomFilter{TEntity,TId,TCount}

        public void Add(TEntity item)
        {
            var id = _configuration.GetId(item);
            var entityHash = _configuration.EntityHash(item);
            _reverseBloomFilters[GetIdx(id, entityHash)].Value.Add(new KeyValuePair<TId, int>(id, entityHash));
        }

        public bool Contains(TEntity item)
        {
            var id = _configuration.GetId(item);
            var entityHash = _configuration.EntityHash(item);
            return _reverseBloomFilters[GetIdx(id, entityHash)].Value.Contains(new KeyValuePair<TId, int>(id, entityHash));
        }

        public bool ContainsKey(TId key)
        {
            throw new NotImplementedException();
        }       

        public InvertibleBloomFilterData<TId, int, TCount> Extract()
        {
            var res = new InvertibleBloomFilterData<TId, int, TCount>();
            res.SubFilters = _reverseBloomFilters.Where(r => r.IsValueCreated).Select(r=> r.Value.Extract()).ToArray();
            res.SubFilterIndexes = _reverseBloomFilters
                .Select((f, i) => new { IsCreated = f.IsValueCreated, Index = i })
                .Where(f => f.IsCreated)
                .Select(f => f.Index)
                .ToArray();
            if (res.SubFilterIndexes.Length == res.SubFilters.Length)
            {
                res.SubFilterIndexes = null;
            }
            res.IsReverse = true;
            res.BlockSize = _blockSize;
            res.HashFunctionCount = _hashFunctionCount;
            return res;
        }

        public void Initialize(long capacity)
        {            
            Initialize(capacity, _configuration.BestErrorRate(capacity));
        }

        public void Initialize(long capacity, float errorRate)
        {
            var internalCapacity = 100L;
            _blockSize = (long)Math.Floor(capacity / 100D);
            if (_blockSize == 0)
            {
                internalCapacity = capacity;
                _blockSize = 1;
            }
            if (_blockSize > 100)
            {
                internalCapacity = _blockSize;
                _blockSize = 100;
            }
            _hashFunctionCount = _configuration.BestHashFunctionCount(internalCapacity, errorRate);
            Initialize(
                _blockSize,
                internalCapacity,
                _configuration.BestSize(internalCapacity, errorRate),
                _hashFunctionCount 
                );
        }

        public void Initialize(long capacity, long m, uint k)
        {
            var internalCapacity = (long)Math.Max(10,Math.Ceiling(1.0D *capacity / m));
            _hashFunctionCount = k;
            Initialize(_blockSize, internalCapacity, m, _hashFunctionCount);
        }

        private void Initialize(long strata, long capacity, long m, uint k)
        {
            //TODO: fix and support long range
            _reverseBloomFilters = Enumerable.Range(0, (int) strata)
                .Select(
                    i =>
                        new Lazy<IInvertibleBloomFilter<KeyValuePair<TId, int>, TId, TCount>>(
                            () =>
                            {
                                var res = new InvertibleReverseBloomFilter<KeyValuePair<TId, int>, TId, TCount>(
                                    _configuration.ValueFilterConfiguration);
                                res.Initialize(capacity, m, k);
                                return res;
                            }))
                .ToArray();
        }

        public void Rehydrate(IInvertibleBloomFilterData<TId, int, TCount> data)
        {           
            if ((data?.SubFilters?.Length??0 )== 0) return;
            _reverseBloomFilters = Enumerable.Range(0, (int) data.BlockSize)
                .Select(
                    i =>
                        new Lazy<IInvertibleBloomFilter<KeyValuePair<TId, int>, TId, TCount>>(
                            () =>
                            {
                                var res = new InvertibleReverseBloomFilter<KeyValuePair<TId, int>, TId, TCount>(
                                    _configuration.ValueFilterConfiguration);
                                res.Rehydrate(data.GetSubFilter(i));
                                return res;
                            }))
                .ToArray();
        }

        public void Remove(TEntity item)
        {
            var id = _configuration.GetId(item);
            var entityHash = _configuration.EntityHash(item);
            _reverseBloomFilters[GetIdx(id, entityHash)].Value.Remove(new KeyValuePair<TId, int>(id, entityHash));
        }

        public void RemoveKey(TId key)
        {
            throw new NotSupportedException();
        }

        public bool SubtractAndDecode(IInvertibleBloomFilter<TEntity, TId, TCount> filter, HashSet<TId> listA, HashSet<TId> listB, HashSet<TId> modifiedEntities)
        {
            var splitFilter = filter as InvertibleReverseSplitBloomFilter<TEntity, TId, TCount>;
            if (splitFilter == null)
            {
                throw new NotSupportedException("SubtractAndDecode only supported when both filters are split or neither filter is split.");
            }
            return SubtractAndDecode(listA, listB, modifiedEntities, splitFilter.Extract());
        }

        public bool SubtractAndDecode(
            HashSet<TId> listA, 
            HashSet<TId> listB, 
            HashSet<TId> modifiedEntities,  
            IInvertibleBloomFilterData<TId, int, TCount> filter)
        {
            if (filter?.SubFilters == null || filter.SubFilters.Length != _reverseBloomFilters.Length)
            {
                throw new ArgumentException("Split Bloom filters should be the same size for subtract and/or decode.", nameof(filter));
            }
            return Extract().SubtractAndDecode(filter, _configuration, listA, listB, modifiedEntities);
        }
        #endregion

        #region Implementation of Bloom Filter public contract

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