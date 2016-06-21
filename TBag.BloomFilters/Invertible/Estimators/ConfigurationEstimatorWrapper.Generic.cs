namespace TBag.BloomFilters.Invertible.Estimators
{
    using System;
    using System.Collections.Generic;
    using HashAlgorithms;
    using Configurations;
    using Invertible;
    using BloomFilters.Configurations;
    using Countable.Configurations;
    /// <summary>
    /// Derived Bloom filter configuration for an estimator
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TCount"></typeparam>
    /// <remarks>Estimators utilize the entity hash as the identifier, so we need to derive a configuration for that,</remarks>
    internal class ConfigurationEstimatorWrapper<TEntity, TId, TCount> :
      KeyValuePairConfigurationBase<int,int,TCount>
         where TCount : struct
        where TId : struct
    {
        private readonly IInvertibleBloomFilterConfiguration<TEntity, TId,  int, TCount> _wrappedConfiguration;
        private Func<KeyValuePair<int, int>, int> _getId;
        private readonly IMurmurHash _murmurHash = new Murmur3();
        private Func<int, int, int> _idAdd;
        private Func<int, int, int> _idRemove;
        private Func<IInvertibleBloomFilterData<int, int, TCount>, long, bool> _isPure;
        private EqualityComparer<int> _idEqualityComparer;
        private Func<KeyValuePair<int, int>, int> _entityHash;
        private Func<int, int> _idHash;
        private Func<int, int, int> _idIntersect;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">The original configuration.</param>
        public ConfigurationEstimatorWrapper(
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration)
        {
            _wrappedConfiguration = configuration;
            _idEqualityComparer = EqualityComparer<int>.Default;
            //ID is a full hash over the key and the value combined.
             _getId = e => BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(e.Value), unchecked((uint) e.Key)), 0);
            //additional hash to ensure Id and IdHash are different.
            _idHash = id => BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(id), 12345678), 0);
            //entity hash equals identifier hash
            _entityHash = e => _idHash(_getId(e));
            //estimator uses XOR
            _idAdd = _idRemove = (id1, id2) => id1 ^ id2;
            _idIntersect = (id1, id2) => id1 & id2;
            _isPure = (d, p) => _wrappedConfiguration.CountConfiguration.IsPure(d.Counts[p]) &&
                                _idHash(d.IdSumProvider[p]) == d.HashSumProvider[p];
        }   

        #region Configuration implementation      

        public override Func<int, int> IdHash {
            get { return _idHash; }
            set { _idHash = value; } }

        public override Func<int, int, int> IdAdd
        {
            get
            {
                return _idAdd;
            }

            set
            {
                _idAdd = value;
            }
        }

        public override Func<int, int, int> IdRemove
        {
            get
            {
                return _idRemove;
            }

            set
            {
               _idRemove = value;
            }
        }

        public override Func<int, int, int> IdIntersect
        {
            get
            {
                return _idIntersect;
            }

            set
            {
                _idIntersect = value;
            }
        }

        public override ICountConfiguration<TCount> CountConfiguration
        {
            get
            {
                return _wrappedConfiguration.CountConfiguration;
            }

            set
            {
                _wrappedConfiguration.CountConfiguration = value;
            }
        }

        public override Func<KeyValuePair<int, int>, int> GetId
        {
            get { return _getId; }
            set { _getId = value; }
        }

        public override EqualityComparer<int> HashEqualityComparer
        {
            get
            {
                return _wrappedConfiguration.HashEqualityComparer;
            }

            set
            {
                _wrappedConfiguration.HashEqualityComparer = value;
            }
        }

        public override Func<int, uint, IEnumerable<int>> Hashes
        {
            get
            {
                return _wrappedConfiguration.Hashes;
            }

            set
            {
                _wrappedConfiguration.Hashes = value;
            }
        }

        public override int HashIdentity
        {
            get
            {
                return _wrappedConfiguration.HashIdentity;
            }

            set
            {
                _wrappedConfiguration.HashIdentity = value;
            }
        }

        public override Func<int, int, int> HashAdd
        {
            get
            {
                return _wrappedConfiguration.HashAdd;
            }

            set
            {
                _wrappedConfiguration.HashAdd = value;
            }
        }

        public override Func<int, int, int> HashRemove
        {
            get
            {
                return _wrappedConfiguration.HashRemove;
            }

            set
            {
                _wrappedConfiguration.HashRemove = value;
            }
        }

        public override Func<int, int, int> HashIntersect
        {
            get
            {
                return _wrappedConfiguration.HashIntersect;
            }

            set
            {
                _wrappedConfiguration.HashIntersect = value;
            }
        }

        public override ICompressedArrayFactory CompressedArrayFactory
        {
            get
            {
                return _wrappedConfiguration.CompressedArrayFactory;
            }

            set
            {
                _wrappedConfiguration.CompressedArrayFactory = value;
            }
        }

        public override EqualityComparer<int> IdEqualityComparer
        {
            get
            {
                return _idEqualityComparer;
            }

            set
            {
                _idEqualityComparer = value;
            }
        }

        public override Func<KeyValuePair<int, int>, int> EntityHash
        {
            get
            {
                return _entityHash;
            }

            set
            {
                _entityHash = value;
            }
        }

        public override int IdIdentity
        { 
            get
            {
                return _wrappedConfiguration.HashIdentity;
            }

            set
            {
                _wrappedConfiguration.HashIdentity = value;
            }
        }

        public override Func<IInvertibleBloomFilterData<int, int, TCount>, long, bool> IsPure
        {
            get
            {
                return _isPure;
            }

            set
            {
                _isPure = value;
            }
        }

        /// <summary>
        /// Override the compressed size.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="errorRate"></param>
        /// <param name="foldFactor"></param>
        /// <returns></returns>
        public override long BestCompressedSize(long capacity, float errorRate, int foldFactor = 0)
        {
            return _wrappedConfiguration.BestCompressedSize(capacity, errorRate, foldFactor);
        }    

        /// <summary>
        /// The folding strategy.
        /// </summary>
        public override IFoldingStrategy FoldingStrategy
        {
            get { return _wrappedConfiguration.FoldingStrategy; }
            set { _wrappedConfiguration.FoldingStrategy = value; }
        }

        public override float BestErrorRate(long capacity)
        {
            return _wrappedConfiguration.BestErrorRate(capacity);
        }

        public override long BestSize(long capacity, float errorRate)
        {
            return _wrappedConfiguration.BestSize(capacity, errorRate);
        }

        public override bool Supports(long capacity, long size)
        {
            return _wrappedConfiguration.Supports(capacity, size);
        }      
        #endregion
    }

}
