using System.Linq;

namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using HashAlgorithms;
    
    /// <summary>
    /// A standard Bloom filter configuration, well suited for Bloom filters that are utilized according to their capacity and store keys rather than key/value pairs.
    /// </summary>
    public abstract class StandardHighCountIbfConfigurationBase<TEntity> : 
        BloomFilterConfigurationBase<TEntity, long, int, int, int>
    {
        private readonly IMurmurHash _murmurHash = new Murmur3();
        private readonly IXxHash _xxHash = new XxHash();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="createValueFilter"></param>
        protected StandardHighCountIbfConfigurationBase(bool createValueFilter = true)
        {
            if (createValueFilter)
            {
                ValueFilterConfiguration = this.ConvertToValueHash();
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        protected StandardHighCountIbfConfigurationBase()
        {
             GetId = GetIdImpl;
            IdHashes = (id, hashCount) =>
            {
                //generate the given number of hashes.
                var murmurHash = BitConverter
                .ToInt32(
                    _murmurHash.Hash(
                        BitConverter.GetBytes(id),
                        unchecked((uint)Math.Abs(id << 4))),
                    0);
                if (hashCount == 1) return new[] { murmurHash };
                var hash2 = BitConverter
                .ToInt32(
                    _xxHash.Hash(
                        BitConverter.GetBytes(id),
                        unchecked((uint)(murmurHash % (uint.MaxValue - 1)))),
                    0);
                return ComputeHash(
                    murmurHash,
                    hash2,
                    hashCount);
            };
            //the hashSum value is an identity hash.
            GetEntityHash = e => IdHashes(GetId(e), 1).First();
            EntityHashes = (e, hashCount) =>
            {
                //generate the given number of hashes.
                var entityHash = GetEntityHash(e);
                var idHash = IdHashes(GetId(e), 1).First();
                var murmurHash = BitConverter.ToInt32(
                    _murmurHash.Hash(
                        BitConverter.GetBytes(entityHash),
                        unchecked((uint)idHash)),
                    0);
                return ComputeHash(murmurHash, idHash, hashCount);
            };
            //TODO: provide a 'standard' version that also includes the hash of the Id stored 
            IsPure = (d, p) => IsPureCount(d.Counts[p]) && EntityHashEqualityComparer.Equals(d.HashSums[p], IdHashes(d.IdSums[p], 1).First());
            IdXor = (id1, id2) => id1 ^ id2;
            EntityHashIdentity = ()=> 0;
            IdIdentity = () => 0L;
            EntityHashXor = (h1, h2) => h1 ^ h2;
            CountUnity = () => 1;
            IsPureCount = c => Math.Abs(c) == 1;
            CountIdentity = () => 0;
            CountDecrease = c => c-1;
            CountIncrease = c => c+1;
            CountSubtract = (c1, c2) => c1 - c2;
            CountEqualityComparer = EqualityComparer<int>.Default;
            IdEqualityComparer = EqualityComparer<long>.Default;
            IdHashEqualityComparer = EqualityComparer<int>.Default;
            EntityHashEqualityComparer = EqualityComparer<int>.Default;
        }

        /// <summary>
        /// Get the identifier (key) for a given entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract long GetIdImpl(TEntity entity);

     

        /// <summary>
        /// Performs Dillinger and Manolios double hashing. 
        /// </summary>
        /// <param name="primaryHash"></param>
        /// <param name="secondaryHash"></param>
        /// <param name="hashFunctionCount"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        private static IEnumerable<int> ComputeHash(
            int primaryHash,
            int secondaryHash,
            uint hashFunctionCount,
            int seed = 0)
        {
            for (long j = seed; j < hashFunctionCount; j++)
            {
                yield return unchecked((int)(primaryHash + j * secondaryHash));
            }
        }
      
        public override bool Supports(ulong capacity, ulong size)
        {    
            return ((sbyte.MaxValue - 15) * size) > capacity;
        }
    }
}

