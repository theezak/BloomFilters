using System.Linq;

namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using HashAlgorithms;
    
    /// <summary>
    /// A default Bloom filter configuration, well suited for  key/value inveritble Bloom filters that are utilized according to their capacity.
    /// </summary>
    public abstract class KeyValueIbfConfigurationBase<TEntity> : 
        BloomFilterConfigurationBase<TEntity, long, int, int, sbyte>
    {
        private readonly IMurmurHash _murmurHash = new Murmur3();
        private readonly IXxHash _xxHash = new XxHash();

        protected KeyValueIbfConfigurationBase(bool createValueFilter = true)
        {
            if (createValueFilter)
            {
                ValueFilterConfiguration = this.ConvertToValueHash();
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        protected KeyValueIbfConfigurationBase()
        {
             GetId = GetIdImpl;
            GetEntityHash = GetEntityHashImpl;
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
            EntityHashes = (e, hashCount) =>
            {
                //generate the given number of hashes.
                var entityHash = GetEntityHashImpl(e);
                var idHash = IdHashes(GetId(e), 1).First();
                var murmurHash = BitConverter.ToInt32(
                    _murmurHash.Hash(
                        BitConverter.GetBytes(entityHash),
                        unchecked((uint)idHash)),
                    0);
                return ComputeHash(murmurHash, idHash, hashCount);
            };
            //TODO: provide a 'standard' version that also includes the hash of the Id stored 
            IsPure = (d, p) => IsPureCount(d.Counts[p]);
            IdXor = (id1, id2) => id1 ^ id2;
            EntityHashIdentity = ()=> 0;
            IdIdentity = () => 0L;
            EntityHashXor = (h1, h2) => h1 ^ h2;
            CountUnity = () => 1;
            IsPureCount = c => Math.Abs(c) == 1;
            CountIdentity = () => 0;
            CountDecrease = c => (sbyte)(c-1);
            CountIncrease = c => (sbyte)(c+1);
            CountSubtract = (c1, c2) => (sbyte)(c1 - c2);
            CountEqualityComparer = EqualityComparer<sbyte>.Default;
            IdEqualityComparer = EqualityComparer<long>.Default;
            IdHashEqualityComparer = EqualityComparer<int>.Default;
            EntityHashEqualityComparer = EqualityComparer<int>.Default;
        }

        /// <summary>
        /// Get the identifier for the given entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract long GetIdImpl(TEntity entity);

        /// <summary>
        /// Get the hash of the entity value.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract int GetEntityHashImpl(TEntity entity);


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

