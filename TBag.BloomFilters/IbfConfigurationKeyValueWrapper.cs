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
    internal class IbfConfigurationKeyValueWrapper<TEntity, TId, THash, TCount> :
          KeyValuePairIbfConfigurationBase<TId, THash, TCount>
           where TCount : struct
            where TId : struct
        where THash : struct
    {
        private readonly IBloomFilterConfiguration<TEntity, TId, THash, TCount> _wrappedConfiguration;
        private readonly IMurmurHash _murmurHash = new Murmur3();
        private readonly IXxHash _xxHash = new XxHash();
        private Func<IInvertibleBloomFilterData<TId, THash, TCount>, long, bool> _isPure;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        public IbfConfigurationKeyValueWrapper(
            IBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration) : 
            base(false)
        {
            _wrappedConfiguration = configuration;
            //hashSum no longer derived from idSum.
            _isPure = (d, position) => _wrappedConfiguration.CountConfiguration.IsPureCount(d.Counts[position]);
        }

        #region Configuration implementation      
        

        public override Func<TId, TId, TId> IdXor
        {
            get
            {
                return _wrappedConfiguration.IdXor;
            }

            set
            {
                _wrappedConfiguration.IdXor = value;
            }
        }

        public override Func<TId, THash> IdHash
        {
            get
            {
                return _wrappedConfiguration.IdHash;
            }

            set
            {
                _wrappedConfiguration.IdHash = value;
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

       

        public override EqualityComparer<THash> HashEqualityComparer
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

        public override Func<THash, THash, uint, IEnumerable<THash>> Hashes
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

        public override Func<THash> HashIdentity
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

        public override Func<THash, THash, THash> HashXor
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

        public override EqualityComparer<TId> IdEqualityComparer
        {
            get
            {
                return _wrappedConfiguration.IdEqualityComparer;
            }

            set
            {
                _wrappedConfiguration.IdEqualityComparer = value;
            }
        }

        public override Func<TId> IdIdentity
        {
            get
            {
                return _wrappedConfiguration.IdIdentity;
            }

            set
            {
                _wrappedConfiguration.IdIdentity = value;
            }
        }

        public override Func<IInvertibleBloomFilterData<TId, THash, TCount>, long, bool> IsPure
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
