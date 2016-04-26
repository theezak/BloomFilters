namespace TBag.BloomFilters.Invertible.Estimators
{
    using Configurations;
    using Invertible;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extension methods for the strata estimator data.
    /// </summary>
    internal static class StrataEstimatorDataExtensions
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
        internal static long? Decode<TEntity, TId, TCount>(this IStrataEstimatorData<int, TCount> data,
            IStrataEstimatorData<int, TCount> otherEstimatorData,
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            byte maxStrata,
            bool destructive = false)
            where TId : struct
            where TCount : struct
        {
            if (data == null || otherEstimatorData == null) return null;
            var strataConfig = configuration.ConvertToEstimatorConfiguration();
            var decodeFactor = Math.Max(data.DecodeCountFactor, otherEstimatorData.DecodeCountFactor);
            var hasDecoded = false;
            var setA = new HashSet<int>();
            var minStrata = Math.Min(data.StrataCount, otherEstimatorData.StrataCount);
            for (var i = minStrata - 1; i >= 0; i--)
            {
                var ibf = data.GetFilterForStrata(i);
                var estimatorIbf = i >= otherEstimatorData.StrataCount
                    ? null
                    : otherEstimatorData.GetFilterForStrata(i);
                if (ibf == null &&
                    estimatorIbf == null)
                {
                    if (i < maxStrata)
                    {
                        hasDecoded = true;
                    }
                    continue;
                }
                var decodeResult = ibf.SubtractAndDecode(estimatorIbf, strataConfig, setA, setA, setA, destructive);
                if (decodeResult != true)
                {
                    if (!hasDecoded) return null;
                    //compensate for the fact that a failed decode can still contribute counts by lowering the i+1 as more decodes succeeded
                    var addedFactor = decodeResult.HasValue ? 1 / Math.Pow(2, data.StrataCount - (i + 1)) : 1;
                    return (long)(Math.Pow(2, i + addedFactor)*decodeFactor*setA.Count);
                }
                hasDecoded = true;
            }
            if (!hasDecoded) return null;
            return (long) (decodeFactor*setA.Count);
        }

        /// <summary>
        /// Intersect the given strata estimators.
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
        internal static StrataEstimatorData<int, TCount> Intersect<TEntity, TId, TCount>(this IStrataEstimatorData<int, TCount> data,
            IStrataEstimatorData<int, TCount> otherEstimatorData,
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration)
            where TId : struct
            where TCount : struct
        {
            if (data == null && otherEstimatorData == null) return null;
            if (data ==null)
            {
                return new StrataEstimatorData<int, TCount>
                {
                    BlockSize = otherEstimatorData.BlockSize,
                    DecodeCountFactor = otherEstimatorData.DecodeCountFactor,
                    HashFunctionCount = otherEstimatorData.HashFunctionCount,
                    StrataCount = otherEstimatorData.StrataCount
                };
            }
            var strataConfig = configuration.ConvertToEstimatorConfiguration();
            var fold = configuration.FoldingStrategy?.GetFoldFactors(data.BlockSize, otherEstimatorData?.BlockSize ?? data.BlockSize);
          var res =  new StrataEstimatorData<int, TCount>
                 {
                     BlockSize = data.BlockSize/(fold?.Item1??1L),
                     BloomFilterStrataIndexes = data.BloomFilterStrataIndexes,
                     BloomFilters = data.BloomFilters?.Select(b=>b.ConvertToBloomFilterData(strataConfig)).ToArray(),
                     DecodeCountFactor = data.DecodeCountFactor,
                     HashFunctionCount = data.HashFunctionCount,
                     StrataCount = data.StrataCount
                 };
          var minStrata = Math.Min(data.StrataCount, otherEstimatorData.StrataCount);
            for (var i = minStrata - 1; i >= 0; i--)
            {
                var ibf = data.GetFilterForStrata(i);
                var estimatorIbf = i >= otherEstimatorData.StrataCount
                    ? null
                    : otherEstimatorData.GetFilterForStrata(i);
                if (ibf == null &&
                    estimatorIbf == null)
                {
                    continue;
                }
                res.BloomFilters[i] = ibf.Intersect(strataConfig, estimatorIbf);
            }
            return res;
        }

        /// <summary>
        /// Lower the strata on the estimator.
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="data"></param>
        /// <param name="newStrata"></param>
        internal static void LowerStrata<TId, TCount>(this StrataEstimatorData<TId, TCount> data, byte newStrata)
            where TId : struct
            where TCount : struct
        {
            if (data == null || newStrata >= data.StrataCount) return;
            var newFilters = new List<IInvertibleBloomFilterData<TId, int, TCount>>(data.BloomFilters);
            var indexes = new List<byte>(data.BloomFilterStrataIndexes ?? new byte[0]);
            for (var i = newStrata; i < data.StrataCount; i++)
            {
                var filter = data.GetFilterForStrata(i);
                if (filter != null)
                {
                    indexes.Remove(i);
                    newFilters.Remove(filter);
                }
            }
            data.BloomFilters = newFilters.Cast<InvertibleBloomFilterData<TId, int, TCount>>().ToArray();
            if (data.BloomFilterStrataIndexes != null)
            {
                data.BloomFilterStrataIndexes = indexes.Count == 0 ? null : indexes.ToArray();
            }
            data.StrataCount = newStrata;
        }

        /// <summary>
        /// Get the Bloom filter for the given strata
        /// </summary>
        /// <typeparam name="TCount">The type for the occurence count</typeparam>
        /// <typeparam name="TId">Type of the identifier</typeparam>
        /// <param name="estimatorData"></param>
        /// <param name="strata"></param>
        /// <returns></returns>
        /// <remarks>Some serializers (*cough* protobuf) simply drop null values from the array. This is mostly harmless work-around.</remarks>
        internal static IInvertibleBloomFilterData<TId, int, TCount> GetFilterForStrata<TId,TCount>(
            this IStrataEstimatorData<TId, TCount> estimatorData, int strata)
            where TCount : struct
            where TId : struct
        {
            if (estimatorData?.BloomFilters == null) return null;
            var indexes = estimatorData.BloomFilterStrataIndexes;
            if (indexes != null && indexes.Length > 0)
            {
                for (var j = indexes.Length - 1; j >= 0; j--)
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

        /// <summary>
        /// Fold the strata estimator data.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="estimatorData"></param>
        /// <param name="configuration"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        internal static StrataEstimatorData<int, TCount> Fold<TEntity, TId, TCount>(
            this IStrataEstimatorData<int, TCount> estimatorData,
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            uint factor)
            where TCount : struct
            where TId : struct
        {
            if (estimatorData?.BloomFilters == null) return null;
            var filterConfig = configuration.ConvertToEstimatorConfiguration();
            var res = new StrataEstimatorData<int, TCount>
            {
                BloomFilters =
                    estimatorData.BloomFilters == null
                        ? null
                        : new InvertibleBloomFilterData<int, int, TCount>[estimatorData.BloomFilters.Length],
                BloomFilterStrataIndexes = estimatorData.BloomFilterStrataIndexes?.ToArray(),
                BlockSize = estimatorData.BlockSize / factor,
                DecodeCountFactor = estimatorData.DecodeCountFactor,
                StrataCount = estimatorData.StrataCount
            };
            for (var j = 0L; j < res.BloomFilters.Length; j++)
            {
                estimatorData
                   .BloomFilters[j]
                   .SyncCompressionProviders(filterConfig);
                res.BloomFilters[j] = estimatorData
                    .BloomFilters[j]
                    .Fold(filterConfig, factor)
                    .ConvertToBloomFilterData(filterConfig);
            }
            return res;
        }

        /// <summary>
        /// Compress the strata estimator data.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="estimatorData"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        internal static StrataEstimatorData<int, TCount> Compress<TEntity, TId, TCount>(
            this IStrataEstimatorData<int, TCount> estimatorData,
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration)
            where TCount : struct
            where TId : struct
        {
            if (configuration?.FoldingStrategy == null || estimatorData == null) return null;
            var fold = configuration.FoldingStrategy?.FindCompressionFactor(configuration, estimatorData.BlockSize, estimatorData.BlockSize,
                estimatorData.ItemCount);
            var res = fold.HasValue ? estimatorData.Fold(configuration, fold.Value) : null;
            return res;
        }
    }
}
