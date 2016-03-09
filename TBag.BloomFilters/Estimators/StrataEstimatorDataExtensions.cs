using System;
using System.Collections.Generic;
using System.Linq;

namespace TBag.BloomFilters.Estimators
{
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
        /// <returns></returns>
        public static ulong Decode<TEntity,TId,TCount>(this IStrataEstimatorData<TId,TCount> data, 
            IStrataEstimatorData<TId,TCount> otherEstimatorData,
            IBloomFilterConfiguration<TEntity,int,TId,long,TCount> configuration)
            where TCount : struct
        {
            if (data == null && otherEstimatorData == null) return 0L;
            if (data == null) return (ulong)otherEstimatorData.Capacity;
            if (otherEstimatorData == null) return (ulong)data.Capacity;
            var setA = new HashSet<TId>();
            for (int i = data.BloomFilters.Length - 1; i >= 0; i--)
            {
                var ibf = data.BloomFilters[i];
                var estimatorIbf = otherEstimatorData.BloomFilters[i];
                if (ibf == null && estimatorIbf == null) continue;
                if (ibf == null || estimatorIbf == null)
                {
                    return (ulong)(Math.Pow(2, i + 1) * data.DecodeCountFactor * Math.Max(setA.Count, 1));
                }
                if (!ibf.SubtractAndDecode(estimatorIbf, configuration, setA, setA, setA))
                {
                    return (ulong)(Math.Pow(2, i + 1) * data.DecodeCountFactor * Math.Max(setA.Count, 1));
                }
            }
            return (ulong)(data.DecodeCountFactor * setA.LongCount());
        }
    }
}
