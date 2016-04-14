namespace TBag.BloomFilters.Invertible.Configurations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Invertible;
    using BloomFilters.Configurations;

    /// <summary>
    /// Extension methods for Bloom filter configuration.
    /// </summary>
    internal static class InvertibleBloomFilterConfigurationExtensions
    {

        /// <summary>
        /// Convert the Bloom filter configuration to a configuration for the value Bloom filter.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="THash">The type of the hash values</typeparam>
        /// <typeparam name="TCount">The type of the occurence count</typeparam>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <returns>The Bloom filter configuration for the value Bloom filter utilized inside the Bloom filter.</returns>
        internal static IInvertibleBloomFilterConfiguration<KeyValuePair<TId, THash>, TId, THash, TCount>
            ConvertToKeyValueHash
            <TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration)
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (configuration == null) return null;
            return new ConfigurationKeyValueHashWrapper<TEntity, TId, THash, TCount>(configuration);
        }

        /// <summary>
        /// Generate the sequence of cell locations to hash the given key to.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="data">The invertible Bloom filter data</param>
        /// <param name="value">The hash value</param>
        /// <returns>A sequence of positions to hash the data to (length equals the number of hash functions configured).</returns>
        internal static IEnumerable<long> Probe<TId, TCount>(
            this IBloomFilterConfiguration<TId, int> configuration,
            IInvertibleBloomFilterData<TId, int, TCount> data,
            int value)
            where TCount : struct
            where TId : struct
        {
            return configuration
                .Hashes(value, data.HashFunctionCount)
                .Select(p => Math.Abs(p%data.BlockSize));
        }
    }
}
