namespace TBag.BloomFilters.Invertible.Configurations
{
    using System.Collections.Generic;
    using System;
    using Invertible;
    using BloomFilters.Configurations;

    /// <summary>
    /// Interface for configuration of a Bloom filter.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The identifier type</typeparam>
    /// <typeparam name="THash">The hash value type</typeparam>
    /// <typeparam name="TCount">The occurence count type.</typeparam>
    /// <remarks>Not the most efficient or elegant implementation, but useful for a test bed.</remarks>
    public interface IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> : 
        IBloomFilterConfiguration<TEntity, TId, THash, TCount>
        where THash : struct
        where TCount : struct
        where TId : struct
    {

        /// <summary>
        /// Configuration for the Bloom filter that hashes values.
        /// </summary>
        /// <remarks>Only used by a hybrid IBF that utilizes both an IBF and a reverse IBF (this being the data for the reverse IBF)</remarks>
        IInvertibleBloomFilterConfiguration<KeyValuePair<TId, THash>, TId, THash, TCount> SubFilterConfiguration { get; }

        /// <summary>
        /// Determine if the location in the given data is pure.
        /// </summary>
        Func<IInvertibleBloomFilterData<TId, THash, TCount>, long, bool> IsPure { get; set; }

        /// <summary>
        /// Data factory
        /// </summary>
        IInvertibleBloomFilterDataFactory DataFactory { get; }
    }
}
