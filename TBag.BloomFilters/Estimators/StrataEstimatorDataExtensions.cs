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
        /// <param name="maxDifference">The maximum number of items in the difference (usually the sum of the set sizes)</param>
        /// <param name="destructive">When <c>true</c> the <paramref name="data"/> will be altered and no longer usable, else <c>false</c></param>
        /// <returns></returns>
        public static ulong Decode<TEntity,TId,TCount>(this IStrataEstimatorData<int,TCount> data, 
            IStrataEstimatorData<int,TCount> otherEstimatorData,
            IBloomFilterConfiguration<TEntity,TId,int,int,TCount> configuration,
            long? maxDifference = null,
            bool destructive = false)
            where TId :struct
            where TCount : struct
        {
            if (data == null && otherEstimatorData == null) return 0L;
            var strataConfig = configuration.ConvertToEntityHashId();
            if (data == null) return (ulong)otherEstimatorData.Capacity;
            if (otherEstimatorData == null) return (ulong)data.Capacity;
            //the difference already seen between the two sets.
            var hasDecoded = false;
            var setA = new HashSet<int>();
            for (int i = data.BloomFilters.Length - 1; i >= 0; i--)
            {
                var ibf = data.BloomFilters[i];
                var estimatorIbf = i >= otherEstimatorData.BloomFilters.Length ? null : otherEstimatorData.BloomFilters[i];
                if (ibf == null && estimatorIbf == null) continue;
                if (ibf == null || estimatorIbf == null)
                {

                    if (!hasDecoded) return (ulong)(maxDifference ?? 1L);
                    return (ulong)(Math.Pow(2, i + 1) * data.DecodeCountFactor * setA.Count);
                }
                if (!ibf.SubtractAndDecode(estimatorIbf, strataConfig, setA, setA, setA, destructive))
                {
                    if (!hasDecoded) return (ulong)(maxDifference ?? 1L);
                    return (ulong)(Math.Pow(2, i + 1) * data.DecodeCountFactor * setA.Count);
                }
                hasDecoded = true;
            }
            if (!hasDecoded) return (ulong)(maxDifference ?? 1L);
            return (ulong)(Math.Max(data.DecodeCountFactor, otherEstimatorData.DecodeCountFactor) * setA.Count);
        }
    }
}
