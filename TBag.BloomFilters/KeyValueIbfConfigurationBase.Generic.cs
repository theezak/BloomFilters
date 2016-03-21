using System.Linq;

namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using HashAlgorithms;
    
    /// <summary>
    /// A standard Bloom filter configuration, well suited for Bloom filters that utilize both keys and values.
    /// </summary>
    public abstract class KeyValueIbfConfigurationBase<TEntity, TCount> :
        StandardIbfConfigurationBase<TEntity, TCount>
        where TCount : struct
    {
        private Func<TEntity, int> _entityHash;
        /// <summary>
        /// Constructor
        /// </summary>
        protected KeyValueIbfConfigurationBase(ICountConfiguration<TCount> configuration, bool createValueFilter = true) :
            base(configuration, createValueFilter)
        {

            //the hashSum value is different.
            EntityHash = GetEntityHashImpl;
            //with the entity hash no longer equal to the Id hash, the definition of pure has to be modified.
            IsPure = (d, p) => CountConfiguration.IsPureCount(d.Counts[p]);
        }

       
        /// <summary>
        /// Implementation for getting the entity hash.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract int  GetEntityHashImpl(TEntity entity);
    }
}

