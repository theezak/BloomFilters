namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using HashAlgorithms;
    using System.Linq;

    /// <summary>
    /// A Bloom filter configuration for Bloom filter that will be highly utilized (more entities are added than it was sized for)
    /// </summary>
    public abstract class KeyValueHighCountIbfConfigurationBase<TEntity> : 
        BloomFilterConfigurationBase<TEntity, long, int, int, int>
    {
        #region Fields
        private readonly IMurmurHash _murmurHash;
        private readonly IXxHash _xxHash;
        private Func<TEntity, long> _getId;
        private Func<TEntity, int> _getEntityHash;
        private Func<long, uint, IEnumerable<int>> _idHashes;
        private Func<TEntity, uint, IEnumerable<int>> _entityHashes;
        private Func<long, long, long> _idXor;
        private Func<int> _entityHashIdentity;
        private Func<long> _idIdentity;
        private Func<int, int, int> _entityHashXor;
        private Func<int> _countUnity;
        private Func<int> _countIdentity;
        private Func<int, bool> _isPureCount;
        private Func<int, int> _countIncrease;
        private Func<int, int> _countDecrease;
        private Func<int, int, int> _countSubtract;
        private Func<IInvertibleBloomFilterData<long, int, int>, long, bool> _isPure;
        private EqualityComparer<int> _countEqualityComparer;
        private EqualityComparer<long> _idEqualityComparer;
        private EqualityComparer<int> _hashEqualityComparer;
        private EqualityComparer<int> _entityHashEqualityComparer;
        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        protected KeyValueHighCountIbfConfigurationBase(bool createValueFilter = true)
        {
            _murmurHash = new Murmur3();
            _xxHash = new XxHash();
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
                            unchecked((uint) Math.Abs(id << 4))),
                        0);
                if (hashCount == 1) return new[] {murmurHash};
                var hash2 = BitConverter
                    .ToInt32(
                        _xxHash.Hash(
                            BitConverter.GetBytes(id),
                            unchecked((uint) (murmurHash%(uint.MaxValue - 1)))),
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
                        unchecked((uint) idHash)),
                    0);
                return ComputeHash(murmurHash, idHash, hashCount);
            };
            _idXor = (id1, id2) => id1 ^ id2;
            _entityHashIdentity = () => 0;
            _idIdentity = () => 0L;
            _entityHashXor = (h1, h2) => h1 ^ h2;
            _countUnity = () => 1;
            _isPureCount = c => Math.Abs(c) == 1;
            _isPure = (d, p) => IsPureCount(d.Counts[p]);
            _countIdentity = () => 0;
            _countDecrease = c => c - 1;
            _countIncrease = c => c + 1;
            _countSubtract = (c1, c2) => c1 - c2;
            _countEqualityComparer = EqualityComparer<int>.Default;
            _idEqualityComparer = EqualityComparer<long>.Default;
            _hashEqualityComparer = EqualityComparer<int>.Default;
            _entityHashEqualityComparer = EqualityComparer<int>.Default;
        }

        #endregion

       
        #region Abstract methods
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
        #endregion

        #region Implementation of Configuration

        public override Func<TEntity, long> GetId {
            get { return _getId; }
            set { _getId = value; }
        }

        protected override Func<TEntity, int> GetEntityHash
        {
            get { return _getEntityHash; }
            set { _getEntityHash = value; }
        }

        public override Func<int, int> CountDecrease {
            get { return _countDecrease; }
            set {_countDecrease = value; }
        }

        public override EqualityComparer<int> CountEqualityComparer {
            get { return _countEqualityComparer; }
            set { _countEqualityComparer = value; }
        }

        public override Func<int> CountIdentity {
            get { return _countIdentity; }
            set { _countIdentity = value; }
        }

        public override Func<int, int> CountIncrease {
            get { return _countIncrease; }
            set { _countIncrease = value; }
        }

        public override Func<int, int, int> CountSubtract {
            get { return _countSubtract; }
            set { _countSubtract = value; }
        }

        public override Func<int> CountUnity {
            get { return _countUnity; }
            set { _countUnity = value; }
        }

        public override EqualityComparer<int> EntityHashEqualityComparer {
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

        public override Func<IInvertibleBloomFilterData<long, int, int>, long, bool> IsPure
        {
            get { return _isPure; }
            set { _isPure = value; }
        }

        public override Func<int, bool> IsPureCount
        {
            get { return _isPureCount; }
            set { _isPureCount = value; }
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
            for (long j = seed; j < hashFunctionCount; j++)
            {
                yield return unchecked((int)Math.Abs(primaryHash + j * secondaryHash));
            }
        }
        #endregion
    }
}

