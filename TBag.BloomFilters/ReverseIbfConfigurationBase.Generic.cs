namespace TBag.BloomFilters
{
    using HashAlgorithms;
    using System;

    /// <summary>
    /// A  Bloom filter configuration well suited for Bloom filters that utilize both keys and values.
    /// </summary>
    public abstract class ReverseIbfConfigurationBase<TEntity, TCount> :
        IbfConfigurationBase<TEntity, TCount>
        where TCount : struct
    {
        #region Fields
        private Func<TEntity, int> _entityHash;
        private Func<IInvertibleBloomFilterData<long, int, TCount>, long, bool> _isPure;
        private readonly IMurmurHash _murmurHash = new Murmur3();
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="createValueFilter"></param>
        protected ReverseIbfConfigurationBase(ICountConfiguration<TCount> configuration, bool createValueFilter = true) :
            base(configuration, createValueFilter)
        {
            //the hashSum value is different.
            _entityHash = e=> unchecked(BitConverter.ToInt32(_murmurHash.Hash(BitConverter.GetBytes(GetEntityHashImpl(e)), (uint)IdHash(GetId(e))), 0));
            //with the entity hash no longer equal to the Id hash, the definition of pure has to be modified.
            _isPure = (d, p) => CountConfiguration.IsPureCount(d.Counts[p]);
        }
        #endregion

        #region Implementation of configuration
        public override Func<TEntity, int> EntityHash
        {
            get { return _entityHash; }
            set { _entityHash = value; }
        }

        public override Func<IInvertibleBloomFilterData<long, int, TCount>, long, bool> IsPure
        {
            get { return _isPure; }
            set { _isPure = value;  }
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// Implementation for getting the entity hash.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>A hash over the value of an entity (does not have to include the identifier).</returns>
        protected abstract int  GetEntityHashImpl(TEntity entity);
        #endregion
    }
}

