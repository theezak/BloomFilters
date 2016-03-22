namespace TBag.BloomFilters.Estimators
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Extension methods for the strata estimator data.
    /// </summary>
    public static class StrataEstimatorDataExtensions
    {
        /// <summary>
        /// Decode the given strata estimators.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the Bloom filter occurence count</typeparam>
        /// <param name="data">Estimator data</param>
        /// <param name="otherEstimatorData">The other estimate</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="destructive">When <c>true</c> the <paramref name="data"/> will be altered and no longer usable, else <c>false</c></param>
        /// <returns></returns>
        public static long? Decode<TEntity,TId,TCount>(this IStrataEstimatorData<int,TCount> data, 
            IStrataEstimatorData<int,TCount> otherEstimatorData,
            IBloomFilterConfiguration<TEntity,TId,int,TCount> configuration,
            bool destructive = false)
            where TId :struct
            where TCount : struct
        {
            if (data == null || otherEstimatorData == null) return null;
            var strataConfig = configuration.ConvertToEstimatorConfiguration();
            var decodeFactor = Math.Max(data.DecodeCountFactor, otherEstimatorData.DecodeCountFactor);
            var hasDecoded = false;
            var setA = new HashSet<int>();
            for (var i = data.BloomFilters.Length - 1; i >= 0; i--)
            {
                var ibf = data.BloomFilters[i];
                var estimatorIbf = i >= otherEstimatorData.BloomFilters.Length ? 
                    null : 
                    otherEstimatorData.BloomFilters[i];
                if (ibf == null && estimatorIbf == null) continue;
                if (ibf == null ||
                    estimatorIbf == null ||
                    !ibf.SubtractAndDecode(estimatorIbf, strataConfig, setA, setA, setA, destructive))
                {
                    if (!hasDecoded) return null;
                     return (long)(Math.Pow(2, i + 1) * decodeFactor * setA.Count);
                }
                hasDecoded = true;
            }
            if (!hasDecoded) return null;
            return (long)(decodeFactor * setA.Count);
        }
    }
}
