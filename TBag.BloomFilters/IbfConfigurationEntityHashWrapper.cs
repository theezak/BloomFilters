namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using HashAlgorithms;

    /// <summary>
    /// Bloom filter configuration for an estimator
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TCount"></typeparam>
    /// <remarks>Estimators utilize the entity hash as the identifier, so we need to derive a configuration for that,</remarks>
    internal class IbfConfigurationEntityHashWrapper<TEntity, TId, TCount> :
        BloomFilterConfigurationBase<TEntity, int, int, int, TCount>
         where TCount : struct
        where TId : struct
    {
        private readonly IBloomFilterConfiguration<TEntity, TId, int, int, TCount> _wrappedConfiguration;
        private Func<TEntity, int> _getId;
        private readonly IMurmurHash _murmurHash = new Murmur3();
        private readonly IXxHash _xxHash = new XxHash();
        private Func<int, int, int> _idXor;
        private Func<IInvertibleBloomFilterData<int, int, TCount>, long, bool> _isPure;
        private Func<int, uint, IEnumerable<int>> _idHashes;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        public IbfConfigurationEntityHashWrapper(
            IBloomFilterConfiguration<TEntity, TId, int, int, TCount> configuration)
        {
            _wrappedConfiguration = configuration;
            //additional hash to ensure Id and entity hash are different.
            _getId = e => BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(_wrappedConfiguration.EntityHashes(e, 1).First()), 12345678), 0);
            _idXor = (id1, id2) => id1 ^ id2;
            //utilizing the fact that the hash sum and id sum should actually be the same, since they are both the first entity hash.
            //This makes the IBF for the strata estimator behave as a standard IBF. 
            _isPure = (d, p) => IsPureCount(d.Counts[p]) && 
                BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(d.HashSums[p]), 12345678), 0) == d.IdSums[p];
            _idHashes = (id, hashCount) =>
            {
                //generate the given number of hashes.
                var murmurHash = BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(id)), 0);
                if (hashCount == 1) return new[] { murmurHash };
                var hash2 = BitConverter
            .ToInt32(_xxHash.Hash(
                    BitConverter.GetBytes(id),
                    unchecked((uint)(murmurHash % (uint.MaxValue - 1)))),
                0);
                return ComputeHash(
                    murmurHash,
                    hash2,
                    hashCount);
            };
        }

        #region Implementation of Ibf configuration

        public override Func<int, int, int> IdXor
        {
            get { return _idXor;}
            set { _idXor = value; }
        }

        public override Func<IInvertibleBloomFilterData<int, int, TCount>, long, bool> IsPure
        {
            get { return _isPure; }
            set { _isPure = value; }
        }

        public override Func<int, uint, IEnumerable<int>> IdHashes
        {
            get { return _idHashes; }
            set { _idHashes = value; }
        }

        public override Func<TCount, TCount> CountDecrease
        {
            get { return _wrappedConfiguration.CountDecrease; }
            set { _wrappedConfiguration.CountDecrease = value; }
        }

        public override Func<TEntity, int> GetId
        {
            get { return _getId; }
            set { _getId = value; }
        }

        public override Func<TCount> CountIdentity
        {
            get { return _wrappedConfiguration.CountIdentity; }
            set { _wrappedConfiguration.CountIdentity = value; }
        }

        public override Func<TCount, TCount> CountIncrease
        {
            get { return _wrappedConfiguration.CountIncrease; }
            set { _wrappedConfiguration.CountIncrease = value; }
        }

        public override Func<TCount, TCount, TCount> CountSubtract
        {
            get { return _wrappedConfiguration.CountSubtract; }
            set { _wrappedConfiguration.CountSubtract = value; }
        }

        public override Func<TCount> CountUnity
        {
            get { return _wrappedConfiguration.CountUnity; }
            set { _wrappedConfiguration.CountUnity = value; }
        }

        public override Func<int, int, int> EntityHashXor
        {
            get { return _wrappedConfiguration.EntityHashXor; }
            set { _wrappedConfiguration.EntityHashXor = value; }
        }


        public override Func<TEntity, uint, IEnumerable<int>> EntityHashes
        {
            get { return _wrappedConfiguration.EntityHashes; }
            set { _wrappedConfiguration.EntityHashes = value; }
        }

        public override Func<TCount, bool> IsPureCount
        {
            get { return _wrappedConfiguration.IsPureCount; }
            set { _wrappedConfiguration.IsPureCount = value; }
        }

        public override Func<int> EntityHashIdentity
        {
            get { return _wrappedConfiguration.EntityHashIdentity; }
            set { _wrappedConfiguration.EntityHashIdentity = value; }
        }

        public override Func<int> IdIdentity
        {
            get { return _wrappedConfiguration.EntityHashIdentity; }
            set { _wrappedConfiguration.EntityHashIdentity = value; }
        }

        public override bool Supports(ulong capacity, ulong size)
        {
            return _wrappedConfiguration.Supports(capacity, size);
        }

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

        public override EqualityComparer<TCount> CountEqualityComparer
        {
            get { return _wrappedConfiguration.CountEqualityComparer; }
            set { _wrappedConfiguration.CountEqualityComparer = value; }
        }

        public override EqualityComparer<int> IdHashEqualityComparer
        {
            get { return _wrappedConfiguration.IdHashEqualityComparer; }
            set { _wrappedConfiguration.IdHashEqualityComparer = value; }
        }

        public override EqualityComparer<int> IdEqualityComparer
        {
            get { return _wrappedConfiguration.EntityHashEqualityComparer; }
            set { _wrappedConfiguration.EntityHashEqualityComparer = value; }
        }

        public override EqualityComparer<int> EntityHashEqualityComparer
        {
            get { return _wrappedConfiguration.EntityHashEqualityComparer; }
            set { _wrappedConfiguration.EntityHashEqualityComparer = value; }
        }
        #endregion
    }

}
