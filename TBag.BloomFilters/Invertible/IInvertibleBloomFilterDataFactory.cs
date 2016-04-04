namespace TBag.BloomFilters.Invertible
{
    using Configurations;
    using System;

    /// <summary>
    /// Interface for the Bloom filter data factory
    /// </summary>
    public interface IInvertibleBloomFilterDataFactory
    {
        /// <summary>
        /// Extract filter data from the given <paramref name="precalculatedFilter"/> for capacity <paramref name="capacity"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The identifier type</typeparam>
        /// <typeparam name="TCount">The occurence count type</typeparam>
        /// <param name="configuration">Configuration</param>
        /// <param name="precalculatedFilter">The pre-calculated filter</param>
        /// <param name="capacity">The targeted capacity.</param>
        /// <returns>The IBF data sized for <paramref name="precalculatedFilter"/> for target capacity <paramref name="capacity"/>.</returns>
        IInvertibleBloomFilterData<TId, int, TCount> Extract<TEntity, TId, TCount>(
           IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            IInvertibleBloomFilter<TEntity, TId, TCount> precalculatedFilter,
           long? capacity)
           where TCount : struct
           where TId : struct;

        /// <summary>
        /// Create Bloom filter data
        /// </summary>
        /// <typeparam name="TId">Type of the entity identifier</typeparam>
        /// <typeparam name="THash">Type of the hash</typeparam>
        /// <typeparam name="TCount">Type of the count occurence</typeparam>
        /// <param name="capacity"></param>
        /// <param name="m">The size (per hash function)</param>
        /// <param name="k">The number of hash functions</param>
        /// <returns>The Bloom filter data.</returns>
        InvertibleBloomFilterData<TId, THash, TCount> Create<TId, THash, TCount>(long capacity, long m, uint k)
            where TId : struct
            where TCount : struct
            where THash : struct;

        /// <summary>
        /// Get the concrete type to serialize to/from.
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <returns></returns>
        Type GetDataType<TId, THash, TCount>()
            where TId : struct
            where THash : struct
            where TCount : struct;

    }
}