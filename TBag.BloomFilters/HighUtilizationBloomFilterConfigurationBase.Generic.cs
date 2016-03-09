namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using TBag.HashAlgorithms;
    
    /// <summary>
    /// A Bloom filter configuration for Bloom filter that will be highly utilized (more entities are added than it was sized for)
    /// </summary>
    public abstract class HighUtilizationBloomFilterConfigurationBase<TEntity> : BloomFilterConfigurationBase<TEntity, int, long, long, int>
    {
        private readonly IMurmurHash _murmurHash;
        private readonly IXxHash _xxHash;

        /// <summary>
        /// Constructor
        /// </summary>
        public HighUtilizationBloomFilterConfigurationBase()
        {
            _murmurHash = new Murmur3();
            _xxHash = new XxHash();
            GetId = e => GetIdImpl(e);
            GetEntityHash = GetEntityHashImpl;
            IdHashes = (id, hashCount) =>
            {
                //generate the given number of hashes.
                var murmurHash = BitConverter.ToInt64(_murmurHash.Hash(BitConverter.GetBytes(id), (uint)Math.Abs(id <<4)),0);
                if (hashCount == 1) return new []{  Math.Abs(murmurHash) };
                var hash2 = BitConverter.ToInt32(_xxHash.Hash(BitConverter.GetBytes(id), (uint)(murmurHash % (uint.MaxValue - 1))), 0);
                return computeHash(murmurHash, hash2, hashCount);
            };
            IdXor = (id1, id2) => id1 ^ id2;
            IsIdIdentity = id1 => id1 == 0;
            IsEntityHashIdentity = id1 => id1 == 0;
            EntityHashXor = (h1, h2) => h1 ^ h2;
            CountUnity = () => 1;
            IsPureCount = c => Math.Abs(c) == 1;
            CountIdentity = () => 0;
            CountDecrease = c => (c-1);
            CountIncrease = c => (c+1);
            CountSubtract = (c1, c2) => (c1 - c2);
        }

        /// <summary>
        /// Implementation for getting the identifier of the given entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract long GetIdImpl(TEntity entity);

        /// <summary>
        /// Implementation for getting the value hash for the given entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract int GetEntityHashImpl(TEntity entity);


        /// <summary>
        /// Performs Dillinger and Manolios double hashing. 
        /// </summary>
        private IEnumerable<long> computeHash(long primaryHash, long secondaryHash, uint hashFunctionCount)
        {
            for (int j = 0; j < hashFunctionCount; j++)
            {
                yield return Math.Abs((primaryHash + (j * secondaryHash)));
            }
        }

        public override bool Supports(ulong capacity, ulong size)
        {
            return ((int.MaxValue - 30) * size) > capacity;
        }
    }
}

