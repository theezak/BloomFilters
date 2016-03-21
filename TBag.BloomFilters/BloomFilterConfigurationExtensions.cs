using System;
using System.Collections.Generic;
using System.Linq;

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
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The entity identifier type</typeparam>
          /// <typeparam name="TCount">The type for the occurrence counter</typeparam>
        /// <returns></returns>
        /// <remarks>Remarkably strange plumbing: for estimators, we want to handle the entity hash as the identifier.</remarks>
        internal static IBloomFilterConfiguration<KeyValuePair<int,int>, int, int, TCount> ConvertToEntityHashId
            <TEntity, TId, TCount>(
            this IBloomFilterConfiguration<TEntity, TId, int,  TCount> configuration)
            where TCount : struct
            where TId : struct
        {
            if (configuration == null) return null;
            return new IbfConfigurationEntityHashWrapper<TEntity,TId, TCount>(configuration);
        }

        /// <summary>
        /// Convert the Bloom filter configuration to a configuration for the value Bloom filter.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
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
            return new IbfConfigurationKeyValueWrapper<TEntity, TId, THash, TCount>(configuration);
        }

        /// <summary>
        /// Generate the sequence of cell locations to hash the given key to.
        /// </summary>
        /// <param name="data">The invertible Bloom filter data</param>
        /// <param name="key">The key</param>
        /// <param name="value">The hash value</param>
        /// <returns></returns>
        internal static IEnumerable<long> Probe<TEntity,TId,TCount>(
            this IBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            IInvertibleBloomFilterData<TId, int, TCount> data,
            TId key, 
           int value)
             where TCount : struct
            where TId : struct
        {
            return configuration
                .Hashes(configuration.IdHash(key), value, data.HashFunctionCount)
                .Select(p => Math.Abs(p % data.Counts.LongLength));
        }
    }
}
