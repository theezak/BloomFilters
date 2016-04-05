namespace TBag.BloomFilters.Invertible.Configurations
{
    using BloomFilters.Configurations;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Bloom filter configuration for a sub filter.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The entity identifier type</typeparam>
    /// <typeparam name="TCount">The type of the occurence count.</typeparam>
    /// <typeparam name="THash">The type of the hash value.</typeparam>
    /// <remarks>The value Bloom filter is reversed, utilizing the value hash as the identifier, and the identifier as the value hash.</remarks>
    internal class ConfigurationKeyValueHashWrapper<TEntity, TId, THash, TCount> :
          KeyValuePairConfigurationBase<TId, THash, TCount>
           where TCount : struct
            where TId : struct
        where THash : struct
    {
        #region Fields
        private readonly IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> _wrappedConfiguration;
        private Func<IInvertibleBloomFilterData<TId, THash, TCount>, long, bool> _isPure;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        public ConfigurationKeyValueHashWrapper(
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration) : 
            base(false)
        {
            _wrappedConfiguration = configuration;
            //hashSum no longer derived from idSum, so pure definition needs to be changed.
            _isPure = (d, position) => _wrappedConfiguration.CountConfiguration.IsPure(d.Counts[position]);
        }
        #endregion

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

        public override Func<THash, uint, IEnumerable<THash>> Hashes
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

        /// <summary>
        /// The best compressed size.
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
        /// The sub filter configuration
        /// </summary>
        public override IInvertibleBloomFilterConfiguration<KeyValuePair<TId, THash>, TId, THash, TCount> SubFilterConfiguration => _wrappedConfiguration.SubFilterConfiguration;

        /// <summary>
        /// The folding strategy.
        /// </summary>
        public override IFoldingStrategy FoldingStrategy {
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
