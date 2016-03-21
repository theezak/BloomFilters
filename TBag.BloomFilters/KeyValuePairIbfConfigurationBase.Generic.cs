namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using HashAlgorithms;
    using System.Linq;

    /// <summary>
    /// A default Bloom filter configuration, well suited for  key/value inveritble Bloom filters that are utilized according to their capacity.
    /// </summary>
    public abstract class KeyValuePairIbfConfigurationBase<TId, THash, TCount> : 
        BloomFilterConfigurationBase<KeyValuePair<TId,THash>, TId, THash, TCount>
        where TId : struct
        where THash : struct
        where TCount : struct
    {
        private Func<KeyValuePair<TId, THash>, TId> _getId;
        private Func<KeyValuePair<TId, THash>, THash> _entityHash;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="createValueFilter">When <c>true</c> a configuration for the RIBF is created as well.</param>
        protected KeyValuePairIbfConfigurationBase(bool createValueFilter = true) : base(createValueFilter)
        {
            _getId = kv => kv.Key;
            _entityHash = kv => kv.Value;
        }

        public override Func<KeyValuePair<TId, THash>, TId> GetId
        {
            get
            {
                return _getId;
            }

            set
            {
                _getId = value;
            }
        }

        public override Func<KeyValuePair<TId, THash>, THash> EntityHash
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
    }
}

