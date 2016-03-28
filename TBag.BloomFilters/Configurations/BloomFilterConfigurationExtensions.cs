namespace TBag.BloomFilters.Configurations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extension methods for Bloom filter configuration.
    /// </summary>
    internal static class BloomFilterConfigurationExtensions
    {
        /// <summary>
        /// Convert a known Bloom filter configuration <paramref name="configuration"/> to a configuration suitable for an estimator.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The entity identifier type</typeparam>
          /// <typeparam name="TCount">The type for the occurrence counter</typeparam>
        /// <returns></returns>
        /// <remarks>Remarkably strange plumbing: for estimators, we want to handle the entity hash as the identifier.</remarks>
        internal static IBloomFilterConfiguration<KeyValuePair<int,int>, int, int, TCount> ConvertToEstimatorConfiguration
            <TEntity, TId, TCount>(
            this IBloomFilterConfiguration<TEntity, TId, int,  TCount> configuration)
            where TCount : struct
            where TId : struct
        {
            if (configuration == null) return null;
            return new IbfConfigurationEstimatorhWrapper<TEntity,TId, TCount>(configuration);
        }

        /// <summary>
        /// Convert the Bloom filter configuration to a configuration for the value Bloom filter.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="THash">The type of the hash values</typeparam>
        /// <typeparam name="TCount">The type of the occurence count</typeparam>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <returns>The Bloom filter configuration for the value Bloom filter utilized inside the Bloom filter.</returns>
        internal static IBloomFilterConfiguration<KeyValuePair<TId,THash>, TId, THash, TCount> ConvertToKeyValueHash
           <TEntity, TId, THash, TCount>(
           this IBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration)
           where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (configuration == null) return null;
            return new IbfConfigurationKeyValueHashWrapper<TEntity, TId, THash, TCount>(configuration);
        }

        /// <summary>
        /// Generate the sequence of cell locations to hash the given key to.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="data">The invertible Bloom filter data</param>
        /// <param name="value">The hash value</param>
        /// <returns>A sequence of positions to hash the data to (length equals the number of hash functions configured).</returns>
        internal static IEnumerable<long> Probe<TEntity,TId,TCount>(
            this IBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            IInvertibleBloomFilterData<TId, int, TCount> data,           
           int value)
             where TCount : struct
            where TId : struct
        {
            return configuration
                .Hashes(value, data.HashFunctionCount)
                .Select(p => Math.Abs(p % data.Counts.LongLength));
        }
    }
}
