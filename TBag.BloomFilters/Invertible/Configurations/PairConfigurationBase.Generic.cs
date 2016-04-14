namespace TBag.BloomFilters.Invertible.Configurations
{
    using BloomFilters.Configurations;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A default Bloom filter configuration, well suited for  key/value pair invertible Bloom filters, where the key is the identifier and the value is a precalculated hash.
    /// </summary>
    public abstract class PairConfigurationBase<TCount> : 
       KeyConfigurationBase<KeyValuePair<long, int>, TCount> 
        where TCount : struct
    {
        #region Fields

        private Func<KeyValuePair<long, int>, int> _entityHash;
        private Func<IInvertibleBloomFilterData<long, int, TCount>, long, bool> _isPure;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="createValueFilter">When <c>true</c> a configuration for the RIBF is created as well.</param>
        protected PairConfigurationBase(ICountConfiguration<TCount> configuration, bool createValueFilter = true) : 
            base(configuration, createValueFilter)
        {
            _entityHash = kv => kv.Value == 0 ? 1 : kv.Value;
            //by changing the entity hash, the pure function needs to be redefined (which increases the potential error rate)
            _isPure = (d, p) => CountConfiguration.IsPure(d.Counts[p]);
        }
        #endregion

        #region Implementation of Configuration

        protected override long GetIdImpl(KeyValuePair<long, int> entity)
        {
            return entity.Key;
        }

        public override Func<KeyValuePair<long, int>, int> EntityHash
        {
            get { return _entityHash;}
            set { _entityHash = value;  }
        }

        public override Func<IInvertibleBloomFilterData<long, int, TCount>, long, bool> IsPure
        {
            get { return _isPure; }
            set { _isPure = value; }
        }
        #endregion
    }
}

