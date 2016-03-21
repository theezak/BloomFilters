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
      KeyValuePairIbfConfigurationBase<int,int,TCount>
         where TCount : struct
        where TId : struct
    {
        private readonly IBloomFilterConfiguration<TEntity, TId,  int, TCount> _wrappedConfiguration;
        private Func<KeyValuePair<int, int>, int> _getId;
        private readonly IMurmurHash _murmurHash = new Murmur3();
        private readonly IXxHash _xxHash = new XxHash();
        private Func<int, int, int> _idXor;
        private Func<IInvertibleBloomFilterData<int, int, TCount>, long, bool> _isPure;
        private EqualityComparer<int> _idEqualityComparer;
        private Func<KeyValuePair<int, int>, int> _entityHash;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        public IbfConfigurationEntityHashWrapper(
            IBloomFilterConfiguration<TEntity, TId,  int, TCount> configuration)
        {
            _wrappedConfiguration = configuration;
            _idEqualityComparer = EqualityComparer<int>.Default;
            //additional hash to ensure Id and entity hash are different.
            _getId = e => BitConverter.ToInt32(
                _murmurHash.Hash(BitConverter.GetBytes(e.Value), 
                unchecked((uint)e.Key)), 0);
            _entityHash = e =>
            {
                return BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(_getId(e)), 12345678), 0);
            };
            _idXor = (id1, id2) => id1 ^ id2;
            //utilizing the fact that the hash sum and id sum should actually be the same, since they are both the first entity hash.
            //This makes the IBF for the strata estimator behave as a standard IBF, but makes it also more selective (decode errors, which are likely, causes it to lower the count)
            _isPure = (d, p) => _wrappedConfiguration.CountConfiguration.IsPureCount(d.Counts[p]) &&
             BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(d.IdSums[p]), 12345678), 0) == d.HashSums[p];
        }

        #region Configuration implementation      


        public override Func<int, int> IdHash { get; set; } = i => i;

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

        public override Func<int, int, uint, IEnumerable<int>> Hashes
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
