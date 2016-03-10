namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using TBag.HashAlgorithms;
    
    /// <summary>
    /// A default Bloom filter configuration, well suited for Bloom filters that are utilized according to their capacity.
    /// </summary>
    public abstract class DefaultBloomFilterConfigurationBase<TEntity> : BloomFilterConfigurationBase<TEntity, int, long, long, sbyte>
    {
        private readonly IMurmurHash _murmurHash;
        private readonly IXxHash _xxHash;

        /// <summary>
        /// Constructor
        /// </summary>
        public DefaultBloomFilterConfigurationBase()
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
            CountDecrease = c => (sbyte)(c>0?c-1:c+1);
            CountIncrease = c => (sbyte)(c>0?c+1:c-1);
            CountSubtract = (c1, c2) => (sbyte)(c1 - c2);
        }

        protected abstract long GetIdImpl(TEntity entity);

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
            return ((sbyte.MaxValue - 15) * size) > capacity;
        }
    }
}

