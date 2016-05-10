

namespace TBag.BloomFilters.Configurations
{
    using System;

    /// <summary>
    /// Extends a <see cref="IBloomFilterConfiguration{TKey, THash}"/> to work with an entity.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="THash"></typeparam>
    public interface IEntityBloomFilterConfiguration<TEntity, TKey, THash> :
        IBloomFilterConfiguration<TKey, THash>
         where TKey : struct
     where THash : struct
    {
        /// <summary>
        /// Function to create a value hash for a given entity.
        /// </summary>
        Func<TEntity, THash> EntityHash { get; set; }

        /// <summary>
        /// Function to get the identifier for a given entity.
        /// </summary>
        Func<TEntity, TKey> GetId { get; set; }


    }
}
