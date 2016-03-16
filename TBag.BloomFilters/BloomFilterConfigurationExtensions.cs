namespace TBag.BloomFilters
{ 
    /// <summary>
    /// Extension methods for Bloom filter configuration.
    /// </summary>
    internal static class BloomFilterConfigurationExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TEntityHash"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="configuration"></param>
        /// <returns></returns>
        /// <remarks>Remarkably strange plumbing: for estimators, we want to handle the entity hash as the identifier.</remarks>
        internal static IBloomFilterConfiguration<TEntity, int, int, int, TCount> ConvertToEntityHashId
            <TEntity, TId, TCount>(
            this IBloomFilterConfiguration<TEntity, TId, int, int, TCount> configuration)
            where TCount : struct
            where TId : struct
        {
            if (configuration == null) return null;
            return new IbfConfigurationEntityHashWrapper<TEntity, TId, TCount>(configuration);
        }

        /// <summary>
        /// Convert the Bloom filter configuration to a configuration for the value Bloom filter.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the occurence count</typeparam>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <returns>The Bloom filter configuration for the value Bloom filter utilized inside the Bloom filter.</returns>
        internal static IBloomFilterConfiguration<TEntity, int, TId, int, TCount> ConvertToValueHash
           <TEntity, TId,  TCount>(
           this IBloomFilterConfiguration<TEntity, TId, int, int, TCount> configuration)
           where TCount : struct
            where TId : struct
        {
            if (configuration == null) return null;
            return new IbfConfigurationReverseWrapper<TEntity, TId, TCount>(configuration);
        }
    }
}
