namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using HashAlgorithms;
    using System.Linq;

    /// <summary>
    /// A default Bloom filter configuration, well suited for  key/value inveritble Bloom filters that are utilized according to their capacity.
    /// </summary>
    public abstract class KeyValueIbfConfigurationBase<TEntity> : 
        BloomFilterConfigurationBase<TEntity, long, int, int, sbyte>
    {
        private readonly IMurmurHash _murmurHash = new Murmur3();
        private readonly IXxHash _xxHash = new XxHash();
        private Func<TEntity, long> _getId;
        private Func<TEntity, int> _getEntityHash;
        private Func<long, uint, IEnumerable<int>> _idHashes;
        private Func<TEntity, uint, IEnumerable<int>> _entityHashes;
        private Func<long, long, long> _idXor;
        private Func<int> _entityHashIdentity;
        private Func<long> _idIdentity;
        private Func<int, int, int> _entityHashXor;
        private Func<sbyte> _countUnity;
        private Func<sbyte> _countIdentity;
        private Func<sbyte, bool> _isPureCount;
        private Func<sbyte, sbyte> _countIncrease;
        private Func<sbyte, sbyte> _countDecrease;
        private Func<sbyte, sbyte, sbyte> _countSubtract;
        private Func<IInvertibleBloomFilterData<long, int, sbyte>, long, bool> _isPure;
        private EqualityComparer<sbyte> _countEqualityComparer;
        private EqualityComparer<long> _idEqualityComparer;
        private EqualityComparer<int> _hashEqualityComparer;
        private EqualityComparer<int> _entityHashEqualityComparer;

        /// <summary>
        /// Constructor
        /// </summary>
        protected KeyValueIbfConfigurationBase(bool createValueFilter = true)
        {
            if (createValueFilter)
            {
                ValueFilterConfiguration = this.ConvertToValueHash();
            }
            _getId = GetIdImpl;
            _getEntityHash = GetEntityHashImpl;
            _idHashes = (id, hashCount) =>
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
                        unchecked((uint)(murmurHash))),
                    0);
                return ComputeHash(
                    murmurHash,
                    hash2,
                    hashCount);
            };
            _entityHashes = (e, hashCount) =>
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
            _isPure = (d, p) => IsPureCount(d.Counts[p]);
            _idXor = (id1, id2) => id1 ^ id2;
            _entityHashIdentity = () => 0;
            _idIdentity = () => 0L;
            _entityHashXor = (h1, h2) => h1 ^ h2;
            _countUnity = () => 1;
            _isPureCount = c => Math.Abs(c) == 1;
            _countIdentity = () => 0;
            _countDecrease = c => (sbyte)(c - 1);
            _countIncrease = c => (sbyte)(c + 1);
            _countSubtract = (c1, c2) => (sbyte)(c1 - c2);
            _countEqualityComparer = EqualityComparer<sbyte>.Default;
            _idEqualityComparer = EqualityComparer<long>.Default;
            _hashEqualityComparer = EqualityComparer<int>.Default;
            _entityHashEqualityComparer = EqualityComparer<int>.Default;
        }

        #region Abstract methods
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
        #endregion

        #region Configuration implementation

        

        /// <summary>
        /// Determine if an IBF, given this configuration and the given <paramref name="capacity"/>, will support a set of the given size.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override bool Supports(ulong capacity, ulong size)
        {    
            return (sbyte.MaxValue - 15) * size > capacity;
        }

        public override Func<TEntity, long> GetId
        {
            get { return _getId; }
            set { _getId = value; }
        }

        protected override Func<TEntity, int> GetEntityHash
        {
            get { return _getEntityHash; }
            set { _getEntityHash = value; }
        }

        public override Func<sbyte, sbyte> CountDecrease
        {
            get { return _countDecrease; }
            set { _countDecrease = value; }
        }

        public override EqualityComparer<sbyte> CountEqualityComparer
        {
            get { return _countEqualityComparer; }
            set { _countEqualityComparer = value; }
        }

        public override Func<sbyte> CountIdentity
        {
            get { return _countIdentity; }
            set { _countIdentity = value; }
        }

        public override Func<sbyte, sbyte> CountIncrease
        {
            get { return _countIncrease; }
            set { _countIncrease = value; }
        }

        public override Func<sbyte, sbyte, sbyte> CountSubtract
        {
            get { return _countSubtract; }
            set { _countSubtract = value; }
        }

        public override Func<sbyte> CountUnity
        {
            get { return _countUnity; }
            set { _countUnity = value; }
        }

        public override EqualityComparer<int> EntityHashEqualityComparer
        {
            get { return _entityHashEqualityComparer; }
            set { _entityHashEqualityComparer = value; }
        }

        public override Func<int> EntityHashIdentity
        {
            get { return _entityHashIdentity; }
            set { _entityHashIdentity = value; }
        }

        public override Func<int, int, int> EntityHashXor
        {
            get { return _entityHashXor; }
            set { _entityHashXor = value; }
        }

        public override Func<TEntity, uint, IEnumerable<int>> EntityHashes
        {
            get { return _entityHashes; }
            set { _entityHashes = value; }
        }

        public override EqualityComparer<long> IdEqualityComparer
        {
            get { return _idEqualityComparer; }
            set { _idEqualityComparer = value; }
        }

        public override EqualityComparer<int> IdHashEqualityComparer
        {
            get { return _hashEqualityComparer; }
            set { _hashEqualityComparer = value; }
        }

        public override Func<long, uint, IEnumerable<int>> IdHashes
        {
            get { return _idHashes; }
            set { _idHashes = value; }
        }

        public override Func<long> IdIdentity
        {
            get { return _idIdentity; }
            set { _idIdentity = value; }
        }

        public override Func<long, long, long> IdXor
        {
            get { return _idXor; }
            set { _idXor = value; }
        }

        public override Func<IInvertibleBloomFilterData<long, int, sbyte>, long, bool> IsPure
        {
            get { return _isPure; }
            set { _isPure = value; }
        }

        public override Func<sbyte, bool> IsPureCount
        {
            get { return _isPureCount; }
            set { _isPureCount = value; }
        }
        #endregion

        #region Methods
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
            for (long j = seed; j < hashFunctionCount+seed; j++)
            {
                yield return unchecked((int)(primaryHash + j * secondaryHash));
            }
        }
        #endregion
    }
}

