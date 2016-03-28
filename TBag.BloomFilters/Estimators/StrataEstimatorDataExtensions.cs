namespace TBag.BloomFilters.Estimators
{
    using Configurations;
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
        /// <param name="maxStrata">The maximum strata</param>
        /// <param name="destructive">When <c>true</c> the <paramref name="data"/> will be altered and no longer usable, else <c>false</c></param>
        /// <returns></returns>
        /// <param name="destructive"></param>
        public static long? Decode<TEntity,TId,TCount>(this IStrataEstimatorData<int,TCount> data, 
            IStrataEstimatorData<int,TCount> otherEstimatorData,
            IBloomFilterConfiguration<TEntity,TId,int,TCount> configuration,
            byte maxStrata,
            bool destructive = false)
            where TId :struct
            where TCount : struct
        {
            if (data == null || otherEstimatorData == null) return null;
            var strataConfig = configuration.ConvertToEstimatorConfiguration();
            var decodeFactor = Math.Max(data.DecodeCountFactor, otherEstimatorData.DecodeCountFactor);
            var hasDecoded = false;
            var setA = new HashSet<int>();
            for (var i = data.StrataCount - 1; i >= 0; i--)
            {
                var ibf = data.GetFilterForStrata(i);
                var estimatorIbf = i >= otherEstimatorData.StrataCount ? 
                    null : 
                    otherEstimatorData.GetFilterForStrata(i);
                if (ibf == null &&
                    estimatorIbf == null)                  
                {
                    if (i < maxStrata)
                    {
                        hasDecoded = true;
                    }
                    continue;
               }
                if (!ibf.SubtractAndDecode(estimatorIbf, strataConfig, setA, setA, setA, destructive))
                {
                    if (!hasDecoded) return null;
                    //compensate for the fact that a failed decode can still contribute counts by lowering the i+1 as more decodes succeeded
                     return (long)(Math.Pow(2, i + (1/Math.Pow(2,  data.StrataCount - (i+1)))) * decodeFactor * setA.Count);
                }
                hasDecoded = true;
            }
            if (!hasDecoded) return null;
            return (long)(decodeFactor * setA.Count);
        }

        /// <summary>
        /// Get the Bloom filter for the given strata
        /// </summary>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="estimatorData"></param>
        /// <param name="strata"></param>
        /// <returns></returns>
        /// <remarks>Some serializers (*cough* protobuf) simply drop null values from the array. This is mostly harmless work-around.</remarks>
        internal static IInvertibleBloomFilterData<int, int, TCount> GetFilterForStrata<TCount>(
            this IStrataEstimatorData<int, TCount> estimatorData, int strata)
            where TCount : struct
        {
            if (estimatorData?.BloomFilters == null) return null;
             var indexes = estimatorData?.BloomFilterStrataIndexes;
            if (indexes != null && indexes.Length > 0)
            {
                for (var j = indexes.Length-1; j >= 0; j--)
                {
                    if (indexes[j] == strata)
                    {
                        return estimatorData.BloomFilters[j];
                    }
                }
            }
            else if (strata < estimatorData.BloomFilters.Length)
            {
                return estimatorData.BloomFilters[strata];
            }
            return null;
        }
    }
}
