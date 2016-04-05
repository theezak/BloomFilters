namespace TBag.BloomFilters.Invertible.Configurations
{
    using System;
    using System.Collections.Generic;
   
    /// <summary>
    /// A default Bloom filter configuration, well suited for  key/value invertible Bloom filters where the key is the identifier and the value is the hash.
    /// </summary>
    /// <remarks>Only internally used. Does not define most functionality except for the identifier (key) and entity hash (value).</remarks>
   internal abstract class KeyValuePairConfigurationBase<TId, THash, TCount> : 
        ConfigurationBase<KeyValuePair<TId,THash>, TId, THash, TCount>
        where TId : struct
        where THash : struct
        where TCount : struct
    {
        #region Fields
        private Func<KeyValuePair<TId, THash>, TId> _getId;
        private Func<KeyValuePair<TId, THash>, THash> _entityHash;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="createValueFilter">When <c>true</c> a configuration for the reverse IBF is created as well.</param>
        protected KeyValuePairConfigurationBase(bool createValueFilter = true) 
            : base(createValueFilter)
        {
            _getId = kv => kv.Key;
            _entityHash = kv => kv.Value;
        }
        #endregion

        #region Implementation of Configuration
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
        #endregion
    }
}

