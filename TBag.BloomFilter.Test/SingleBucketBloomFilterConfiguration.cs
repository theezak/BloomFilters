using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBag.BloomFilters;
using TBag.HashAlgorithms;

namespace TBag.BloomFilter.Test
{
    /// <summary>
    /// A test Bloom filter configuration.
    /// </summary>
    internal class SingleBucketBloomFilterConfiguration : BloomFilterConfigurationBase<TestEntity, int, long, long, byte>
    {
        private readonly IMurmurHash _murmurHash;
        private readonly IXxHash _xxHash;

        /// <summary>
        /// Constructor
        /// </summary>
        public SingleBucketBloomFilterConfiguration()
        {
            _murmurHash = new Murmur3();
            _xxHash = new XxHash();
            GetId = e => e.Id;
            GetEntityHash = entity => BitConverter.ToInt32(_murmurHash.Hash(Encoding.Unicode.GetBytes($"{entity.Id}::{entity.Value}")), 0);
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
            CountDecrease = c => (byte)(c-1);
            CountIncrease = c => (byte)(c+1);
            CountSubtract = (c1, c2) => (byte)(c1 - c2);
        }

        public override bool Supports(ulong capacity, ulong size)
        {
            return ((byte.MaxValue-15) * size) > capacity;
        }



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
    }
}

