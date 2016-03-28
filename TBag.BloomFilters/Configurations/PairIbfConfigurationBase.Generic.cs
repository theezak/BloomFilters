namespace TBag.BloomFilters.Configurations
{
    using System;
    using System.Collections.Generic;
   
    /// <summary>
    /// A default Bloom filter configuration, well suited for  key/value inveritble Bloom filters that are utilized according to their capacity.
    /// </summary>
    public abstract class PairIbfConfigurationBase<TCount> : 
       IbfConfigurationBase<KeyValuePair<long, int>, TCount> 
        where TCount : struct
    {
        #region Fields
        private Func<KeyValuePair<long, int>, long> _getId;
        private Func<KeyValuePair<long, int>, int> _entityHash;
        private Func<IInvertibleBloomFilterData<long, int, TCount>, long, bool> _isPure;

        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="createValueFilter">When <c>true</c> a configuration for the RIBF is created as well.</param>
        protected PairIbfConfigurationBase(ICountConfiguration<TCount> configuration, bool createValueFilter = true) : 
            base(configuration, createValueFilter)
        {
            _entityHash = kv => kv.Value;
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

