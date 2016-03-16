namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using HashAlgorithms;

    /// <summary>
    /// Bloom filter configuration for a value Bloom filter.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TCount"></typeparam>
    /// <remarks>The value Bloom filter is reversed, utilizing the value hash as the identifier, and the identifier as the value hash.</remarks>
    internal class IbfConfigurationReverseWrapper<TEntity, TId, TCount> :
           BloomFilterConfigurationBase<TEntity, int, TId, int, TCount>
           where TCount : struct
            where TId : struct
    {
        private readonly IBloomFilterConfiguration<TEntity, TId, int, int, TCount> _wrappedConfiguration;
        private readonly IMurmurHash _murmurHash = new Murmur3();
        private readonly IXxHash _xxHash = new XxHash();
        private Func<int, int, int> _idXor;
        private Func<int, uint, IEnumerable<int>> _idHashes;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        public IbfConfigurationReverseWrapper(
            IBloomFilterConfiguration<TEntity, TId, int, int, TCount> configuration)
        {
            _wrappedConfiguration = configuration;
            _idXor = (id1, id2) => id1 ^ id2;
            _idHashes = (id, hashCount) =>
            {
                //generate the given number of hashes.
                var murmurHash = BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(id)), 0);
                if (hashCount == 1) return new[] { murmurHash };
                var hash2 = BitConverter.ToInt32(_xxHash.Hash(
                    BitConverter.GetBytes(id),
                    unchecked((uint)(murmurHash % (uint.MaxValue - 1)))),
                0);
                return ComputeHash(murmurHash, hash2, hashCount);
            };
        }

        #region Configuration implementation      
        public override Func<TCount, TCount> CountDecrease
        {
            get { return _wrappedConfiguration.CountDecrease; }
            set { _wrappedConfiguration.CountDecrease = value; }
        }

        public override Func<TEntity, int> GetId
        {
            get { return e => _wrappedConfiguration.EntityHashes(e, 1).First(); }
            set
            {
                throw new NotImplementedException("Setting the GetId method on a derived configuration for value hashing is not supported.");
            }
        }

        public override Func<int, int, int> IdXor {
            get { return _idXor; }
            set { _idXor = value; }
        }

        public override Func<int, uint, IEnumerable<int>> IdHashes {
            get { return _idHashes; }
            set { _idHashes = value; }
        }

        public override Func<IInvertibleBloomFilterData<int, TId, TCount>, long, bool> IsPure
        {
            get
            {
                return (d, p) => _wrappedConfiguration.IsPure(d.Reverse(), p);
            }

            set
            {
                throw new NotImplementedException("Setting the IsPure method on a derived configuration for value hashing is not supported.");
            }
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

        public override Func<TId, TId, TId> EntityHashXor
        {
            get { return _wrappedConfiguration.IdXor; }
            set { _wrappedConfiguration.IdXor = value; }
        }

        public override Func<TEntity, uint, IEnumerable<TId>> EntityHashes
        {
            get { return GetEntityHashes; }
            set
            {
                throw new NotImplementedException("Setting the EntityHashes method on a derived configuration for value hashing is not supported.");
            }
        }

        public override Func<TCount, bool> IsPureCount
        {
            get { return _wrappedConfiguration.IsPureCount; }
            set { _wrappedConfiguration.IsPureCount = value; }
        }

        public override Func<TId> EntityHashIdentity
        {
            get { return _wrappedConfiguration.IdIdentity; }
            set { _wrappedConfiguration.IdIdentity = value; }
        }

        public override Func<int> IdIdentity
        {
            get { return _wrappedConfiguration.EntityHashIdentity; }
            set { _wrappedConfiguration.EntityHashIdentity = value; }
        }
        public override EqualityComparer<TCount> CountEqualityComparer
        {
            get { return _wrappedConfiguration.CountEqualityComparer; }
            set { _wrappedConfiguration.CountEqualityComparer = value; }
        }

        public override EqualityComparer<TId> EntityHashEqualityComparer
        {
            get { return _wrappedConfiguration.IdEqualityComparer; }
            set { _wrappedConfiguration.IdEqualityComparer = value; }
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

        public override bool Supports(ulong capacity, ulong size)
        {
            return _wrappedConfiguration.Supports(capacity, size);
        }
        #endregion

        #region Methods
        private IEnumerable<TId> GetEntityHashes(TEntity entity, uint count)
        {
            var id = _wrappedConfiguration.GetId(entity);
            for (var j = 0; j < count; j++)
            {
                yield return id;
            }
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
        #endregion
    }
}
