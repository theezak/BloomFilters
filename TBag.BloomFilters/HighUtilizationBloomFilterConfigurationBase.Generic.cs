namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using HashAlgorithms;
    using System.Linq;

    /// <summary>
    /// A Bloom filter configuration for Bloom filter that will be highly utilized (more entities are added than it was sized for)
    /// </summary>
    public abstract class HighUtilizationBloomFilterConfigurationBase<TEntity> : 
        BloomFilterConfigurationBase<TEntity, long, int, int, int>
    {
        private readonly IMurmurHash _murmurHash;
        private readonly IXxHash _xxHash;

        /// <summary>
        /// Constructor
        /// </summary>
        protected HighUtilizationBloomFilterConfigurationBase(bool createValueFilter = true)
        {
            _murmurHash = new Murmur3();
            _xxHash = new XxHash();       
            if (createValueFilter)
            {
                ValueFilterConfiguration = this.ConvertToValueHash();
            }
        GetId = GetIdImpl;
            GetEntityHash = GetEntityHashImpl;
            IdHashes = (id, hashCount) =>
            {
                //generate the given number of hashes.
                var murmurHash = BitConverter
                .ToInt32(
                    _murmurHash.Hash(
                        BitConverter.GetBytes(id), 
                        unchecked((uint)Math.Abs(id <<4))),
                    0);
                if (hashCount == 1) return new []{  murmurHash };
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
            IdXor = (id1, id2) => id1 ^ id2;
            IsIdIdentity = id1 => id1 == 0;
            IsEntityHashIdentity = id1 => id1 == 0;
            EntityHashXor = (h1, h2) => h1 ^ h2;
            CountUnity = () => 1;
            IsPureCount = c => Math.Abs(c) == 1;
            CountIdentity = () => 0;
            CountDecrease = c => (c<0?c+1:c-1);
            CountIncrease = c => (c < 0 ? c - 1 : c + 1);
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
            for(long j = seed; j < hashFunctionCount; j++)
            {
                yield return unchecked((int)Math.Abs(primaryHash + j * secondaryHash));
            }
        }

        /// <summary>
        /// Determine if the capacity of the Bloom filter supports the set size.
        /// </summary>
        /// <param name="capacity">Bloom filter capacity.</param>
        /// <param name="size">Set size</param>
        /// <returns><c>false</c> when the set size is likely to cause overflows in the count, else <c>trye</c></returns>
        public override bool Supports(ulong capacity, ulong size)
        {
            return (int.MaxValue - 30) * size > capacity;
        }
    }
}

