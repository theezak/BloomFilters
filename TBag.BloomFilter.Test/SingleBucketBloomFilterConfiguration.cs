using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBag.BloomFilters;
using TBag.HashAlgorithms;

namespace TBag.BloomFilter.Test
{
    internal class SingleBucketBloomFilterConfiguration : BloomFilterConfiguration<TestEntity, int, long, int>
    {
        private readonly IMurmurHash _murmurHash;
        private readonly IXxHash _xxHash;
        public SingleBucketBloomFilterConfiguration()
        {
            _murmurHash = new Murmur3();
            _xxHash = new XxHash();
            GetId = e => e.Id;
            GetEntityHash = entity => BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(entity.Value)), 0);
            IdHashes = (id, hashCount) =>
            {
                //generate the given number of hashes.
                var hash1 = BitConverter.ToInt32(_xxHash.Hash(BitConverter.GetBytes(id)), 0);
                var hash2 = BitConverter.ToInt64(_murmurHash.Hash(BitConverter.GetBytes(id), (uint)hash1), 0).GetHashCode();
                return computeHash(hash1, hash2, hashCount);
            };
            IdXor = (id1, id2) => id1 ^ id2;
            IsIdIdentity = id1 => id1 == 0;
            IsHashIdentity = id1 => id1 == 0;
            IsIdHashIdentity = id1 => id1 == 0;
            EntityHashXor = (h1, h2) => h1 ^ h2;
        }



        /// <summary>
        /// Performs Dillinger and Manolios double hashing. 
        /// </summary>
        private IEnumerable<int> computeHash(int primaryHash, int secondaryHash, uint hashFunctionCount)
        {
            for (int j = 0; j < hashFunctionCount; j++)
            {
                yield return Math.Abs((primaryHash + (j * secondaryHash)));
            }
        }
    }
}

