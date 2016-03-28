namespace TBag.BloomFilters.Configurations
{
    using HashAlgorithms;
    using System;
  
    /// <summary>
    /// A test Bloom filter configuration for hybrid Bloom filter.
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity</typeparam>
    /// <typeparam name="TCount">Type of the occurence count</typeparam>
    /// <remarks>Generates a full entity hash while keeping the standard pure implementation, knowing that the hybrid IBF won't use the entity hash except for internal the reverse IBF.</remarks>
    public abstract class HybridIbfConfiguration<TEntity,TCount> : IbfConfigurationBase<TEntity, TCount>
        where TCount : struct
    {
        private readonly IMurmurHash _murmurHash = new Murmur3();
         private Func<TEntity, int> _entityHash;

       /// <summary>
       /// Constructor
       /// </summary>
       /// <param name="countConfiguration"></param>
       /// <param name="createFilter"></param>
        public HybridIbfConfiguration(ICountConfiguration<TCount> countConfiguration,
            bool createFilter = true) : base(countConfiguration, createFilter)
        {
            //set the custom hash: the hybrid IBF will only use the IdHash (with the pure definition that includes count and hashSum)
            //the reverse IBF will however get the entityHash (and will use a pure definition that only includes the count)
            _entityHash = e => unchecked(GetEntityHashImpl(e) + 3 * IdHash(GetId(e)));
        }

        /// <summary>
        /// Get the entity hash
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract int GetEntityHashImpl(TEntity entity);

        /// <summary>
        /// The entity hash.
        /// </summary>
        public override Func<TEntity, int> EntityHash {
            get { return _entityHash;}
            set { _entityHash = value; }
        }
    }
}