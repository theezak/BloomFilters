using System;

namespace TBag.BloomFilters
{
    /// <summary>
    /// Interface for the Bloom filter data factory
    /// </summary>
    public interface IInvertibleBloomFilterDataFactory
    {
        /// <summary>
        /// Create Bloom filter data
        /// </summary>
        /// <typeparam name="TId">Type of the entity identifier</typeparam>
        /// <typeparam name="THash">Type of the hash</typeparam>
        /// <typeparam name="TCount">Type of the count occurence</typeparam>
        /// <param name="m">The size (per hash function)</param>
        /// <param name="k">The number of hash functions</param>
        /// <returns>The Bloom filter data.</returns>
        InvertibleBloomFilterData<TId, THash, TCount> Create<TId, THash, TCount>(long m, uint k)
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