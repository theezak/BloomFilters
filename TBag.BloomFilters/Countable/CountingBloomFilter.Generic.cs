using System;
using System.Collections.Generic;
using TBag.BloomFilters.Countable.Configurations;

namespace TBag.BloomFilters.Countable
{
    class CountingBloomFilter<TEntity, TKey, TCount> :
        ICountingBloomFilter<TEntity, TKey, TCount>
        where TKey : struct
        where TCount : struct
    {
        public long BlockSize
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public long Capacity
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEntityCountingBloomFilterConfiguration<TEntity, TKey, int, TCount> Configuration
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public float ErrorRate
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public uint HashFunctionCount
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public long ItemCount
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Add(ICountingBloomFilterData<TKey, TCount> bloomFilterData)
        {
            throw new NotImplementedException();
        }

        public void Add(ICountingBloomFilter<TEntity, TKey, TCount> bloomFilter)
        {
            throw new NotImplementedException();
        }

        public void Add(TEntity item)
        {
            throw new NotImplementedException();
        }

        public ICountingBloomFilter<TEntity, TKey, TCount> Compress(bool inPlace = false)
        {
            throw new NotImplementedException();
        }

        public bool Contains(TEntity item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(TKey key)
        {
            throw new NotImplementedException();
        }

        public ICountingBloomFilterData<TKey, TCount> Extract()
        {
            throw new NotImplementedException();
        }

        public ICountingBloomFilter<TEntity, TKey, TCount> Fold(uint factor, bool destructive = false)
        {
            throw new NotImplementedException();
        }

        public void Initialize(long capacity, int foldFactor = 0)
        {
            throw new NotImplementedException();
        }

        public void Initialize(long capacity, long m, uint k)
        {
            throw new NotImplementedException();
        }

        public void Initialize(long capacity, float errorRate, int foldFactor = 0)
        {
            throw new NotImplementedException();
        }

        public void Intersect(ICountingBloomFilterData<TKey, TCount> otherFilterData)
        {
            throw new NotImplementedException();
        }

        public void Intersect(ICountingBloomFilter<TEntity, TKey, TCount> bloomFilter)
        {
            throw new NotImplementedException();
        }

        public void Rehydrate(ICountingBloomFilterData<TKey, TCount> data)
        {
            throw new NotImplementedException();
        }

        public void Remove(TEntity item)
        {
            throw new NotImplementedException();
        }

        public void RemoveKey(TKey key)
        {
            throw new NotImplementedException();
        }

        public bool? SubtractAndDecode(ICountingBloomFilter<TEntity, TKey, TCount> filter, HashSet<TKey> listA, HashSet<TKey> listB, HashSet<TKey> modifiedEntities)
        {
            throw new NotImplementedException();
        }

        public bool? SubtractAndDecode(HashSet<TKey> listA, HashSet<TKey> listB, HashSet<TKey> modifiedEntities, ICountingBloomFilterData<TKey, TCount> filterData)
        {
            throw new NotImplementedException();
        }
    }
}
