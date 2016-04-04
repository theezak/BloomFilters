namespace TBag.BloomFilters.Invertible.Estimators
{
    using Configurations;
    using System.Collections.Generic;
   
    /// <summary>
    /// Bloom filter configuration extensions
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
        internal static IInvertibleBloomFilterConfiguration<KeyValuePair<int, int>, int, int, TCount> ConvertToEstimatorConfiguration
            <TEntity, TId, TCount>(
            this IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration)
            where TCount : struct
            where TId : struct
        {
            if (configuration == null) return null;
            return new ConfigurationEstimatorWrapper<TEntity, TId, TCount>(configuration);
        }
    }
}
