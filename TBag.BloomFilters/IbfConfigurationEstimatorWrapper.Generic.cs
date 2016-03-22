namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using HashAlgorithms;

    /// <summary>
    /// Derived Bloom filter configuration for an estimator
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TCount"></typeparam>
    /// <remarks>Estimators utilize the entity hash as the identifier, so we need to derive a configuration for that,</remarks>
    internal class IbfConfigurationEstimatorhWrapper<TEntity, TId, TCount> :
      KeyValuePairIbfConfigurationBase<int,int,TCount>
         where TCount : struct
        where TId : struct
    {
        private readonly IBloomFilterConfiguration<TEntity, TId,  int, TCount> _wrappedConfiguration;
        private Func<KeyValuePair<int, int>, int> _getId;
        private readonly IMurmurHash _murmurHash = new Murmur3();
        private Func<int, int, int> _idXor;
        private Func<IInvertibleBloomFilterData<int, int, TCount>, long, bool> _isPure;
        private EqualityComparer<int> _idEqualityComparer;
        private Func<KeyValuePair<int, int>, int> _entityHash;
        private Func<int, int> _idHash;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">The original configuration.</param>
        public IbfConfigurationEstimatorhWrapper(
            IBloomFilterConfiguration<TEntity, TId, int, TCount> configuration)
        {
            _wrappedConfiguration = configuration;
            _idEqualityComparer = EqualityComparer<int>.Default;
            //ID is a full hash over the key and the value combined.
             _getId = e => BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(e.Value), unchecked((uint) e.Key)), 0);
            //additional hash to ensure Id and IdHash are different.
            _idHash = id => BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(id), 12345678), 0);
            //entity hash equals identifier hash/
            _entityHash = e => BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(_getId(e)), 12345678), 0);
            _idXor = (id1, id2) => id1 ^ id2;
            _isPure = (d, p) => _wrappedConfiguration.CountConfiguration.IsPureCount(d.Counts[p]) &&
                                BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(d.IdSums[p]), 12345678), 0) ==
                                d.HashSums[p];
        }   

        #region Configuration implementation      

        public override Func<int, int> IdHash {
            get { return _idHash; }
            set { _idHash = value; } }

        public override Func<int, int, int> IdXor
        {
            get
            {
                return _idXor;
            }

            set
            {
                _idXor = value;
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

        public override Func<int> HashIdentity
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

        public override Func<int, int, int> HashXor
        {
            get
            {
                return _wrappedConfiguration.HashXor;
            }

            set
            {
                _wrappedConfiguration.HashXor = value;
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

        public override Func<int> IdIdentity
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

        public override long BestCompressedSize(long capacity, float errorRate)
        {
            return _wrappedConfiguration.BestCompressedSize(capacity, errorRate);
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
