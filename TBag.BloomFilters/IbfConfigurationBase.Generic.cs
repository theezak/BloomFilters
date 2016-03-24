using System.Linq;

namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using HashAlgorithms;
    
    /// <summary>
    /// A standard Bloom filter configuration, well suited for Bloom filters that are utilized according to their capacity and store keys rather than key/value pairs.
    /// </summary>
    public abstract class IbfConfigurationBase<TEntity, TCount> : 
        BloomFilterConfigurationBase<TEntity, long, int, TCount>
        where TCount : struct
    {
        #region Fields
        private readonly IMurmurHash _murmurHash = new Murmur3();
        private Func<TEntity, long> _getId;
        private Func<TEntity, int> _entityHash;
        private Func<long, long, long> _idXor;
        private Func<int> _hashIdentity;
        private Func<long> _idIdentity;
        private Func<int, int, int> _hashXor;
        private Func<IInvertibleBloomFilterData<long, int, TCount>, long, bool> _isPure;
        private EqualityComparer<long> _idEqualityComparer;
         private EqualityComparer<int> _hashEqualityComparer;
        private Func<int, uint, IEnumerable<int>> _hashes;
        private ICountConfiguration<TCount> _countConfiguration;
        private Func<long, int> _idHash;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        protected IbfConfigurationBase(ICountConfiguration<TCount> configuration, bool createValueFilter = true) : 
            base(createValueFilter)
        {
            _countConfiguration = configuration;
            _getId = GetIdImpl;
            _idHash = id => BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(id)), 0);
            _hashes = (hash, hashCount) =>  ComputeHash(hash, BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(hash), 912345678), 0), hashCount, 1);
            //the hashSum value is an identity hash.
            _entityHash = e => IdHash(GetId(e));
            _isPure = (d, p) => CountConfiguration.IsPureCount(d.Counts[p]) && HashEqualityComparer.Equals(d.HashSums[p], IdHash(d.IdSums[p]));
            _idXor = (id1, id2) => id1 ^ id2;
            _hashIdentity = () => 0;
            _idIdentity = () => 0L;
            _hashXor = (h1, h2) => h1 ^ h2;
            _idEqualityComparer = EqualityComparer<long>.Default;
            _hashEqualityComparer = EqualityComparer<int>.Default;
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// Get the identifier for a given entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract long GetIdImpl(TEntity entity);
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
        private static int[] ComputeHash(
            int primaryHash,
            int secondaryHash,
            uint hashFunctionCount,
            int seed = 0)
        {
            return HashGenerator(primaryHash, secondaryHash, seed).Distinct().Take((int)hashFunctionCount).ToArray();
        }

        private static IEnumerable<int> HashGenerator(
             int primaryHash,
             int secondaryHash,            
             int seed = 0)
        {          
            for (long j = seed; j < long.MaxValue; j++)
            {
                yield return unchecked((int)(primaryHash + j * secondaryHash));
            }
        }
        #endregion

        #region Configuration implementation

        public override Func<long, int> IdHash
        {
            get { return _idHash; }

            set { _idHash = value; }
        }

        public override ICountConfiguration<TCount> CountConfiguration
        {
            get { return _countConfiguration; }
            set { _countConfiguration = value; }
        }

        public override Func<TEntity, long> GetId
        {
            get { return _getId; }
            set { _getId = value; }
        }

        public override Func<TEntity, int> EntityHash
        {
            get { return _entityHash; }
            set { _entityHash = value; }
        }

        public override Func<int, uint, IEnumerable<int>> Hashes
        {
            get { return _hashes; }
            set { _hashes = value; }
        }

        public override EqualityComparer<int> HashEqualityComparer
        {
            get { return _hashEqualityComparer; }
            set { _hashEqualityComparer = value; }
        }

        public override Func<int> HashIdentity
        {
            get { return _hashIdentity; }
            set { _hashIdentity = value; }
        }

        public override Func<int, int, int> HashXor
        {
            get { return _hashXor; }
            set { _hashXor = value; }
        }

        public override EqualityComparer<long> IdEqualityComparer
        {
            get { return _idEqualityComparer; }
            set { _idEqualityComparer = value; }
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

        public override Func<IInvertibleBloomFilterData<long, int, TCount>, long, bool> IsPure
        {
            get { return _isPure; }
            set { _isPure = value; }
        }
       #endregion
    }
}

