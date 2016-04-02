namespace TBag.BloomFilters.Estimators
{
    using Configurations;
    using MathExt;
    using System;
    using System.Collections;
    using System.Linq;
    /// <summary>
    /// Extension methods for the hybrid estimator data.
    /// </summary>
    public static class HybridEstimatorDataExtensions
    {
        /// <summary>
        /// Maximum number of trailing zeros (based upon the hash type)
        /// </summary>
        private const byte MaxTrailingZeros = sizeof(int) * 8;

        /// <summary>
        /// Decode the hybrid estimator data instances.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the occurence count for the Bloom filter.</typeparam>
        /// <param name="estimator">The estimator</param>
        /// <param name="otherEstimatorData">The other estimator</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="destructive">When <c>true</c> the values of <paramref name="estimator"/> will be altered rendering it useless, otherwise <c>false</c></param>
        /// <returns>An estimate of the difference between two sets based upon the estimators.</returns>
        public static long? Decode<TEntity, TId, TCount>(this IHybridEstimatorData<int, TCount> estimator,
            IHybridEstimatorData<int, TCount> otherEstimatorData,
            IBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            bool destructive = false)
            where TCount : struct
            where TId : struct
        {
            if (estimator == null &&
                otherEstimatorData == null) return 0L;
            if (estimator == null ||
                estimator.ItemCount <= 0L)
                return otherEstimatorData.ItemCount;
            if (otherEstimatorData == null ||
                otherEstimatorData.ItemCount <= 0)
                return estimator.ItemCount;
            var decodeFactor = Math.Max(estimator.StrataEstimator?.DecodeCountFactor ?? 1.0D,
                otherEstimatorData.StrataEstimator?.DecodeCountFactor ?? 1.0D);
            var strataDecode = estimator
                .StrataEstimator
                .Decode(otherEstimatorData.StrataEstimator, configuration, estimator.StrataCount, destructive);
            if (!strataDecode.HasValue) return null;
            var similarity = estimator.BitMinwiseEstimator?.Similarity(otherEstimatorData.BitMinwiseEstimator);
            if (similarity.HasValue)
            {
                strataDecode += (long)(decodeFactor * ((1 - similarity) / (1 + similarity)) *
                       (estimator.BitMinwiseEstimator.Capacity + otherEstimatorData.BitMinwiseEstimator.Capacity));
            }
            var decodedItemCount = estimator.StrataEstimator.ItemCount + (estimator.BitMinwiseEstimator?.ItemCount ?? 0L);
            if (strataDecode.HasValue &&
                decodedItemCount > 0)
            {
                //assume differences for the items counted, but not in the strata estimator or bit minwise estimator, contribute proportionally.
                strataDecode = (long)Math.Ceiling(1.0D * strataDecode.Value * estimator.ItemCount / decodedItemCount);
            }
            //use upperbound on set difference.
            return Math.Min(strataDecode.Value, estimator.ItemCount + otherEstimatorData.ItemCount);
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
        public static HybridEstimatorFullData<int, TCount> Fold<TEntity, TId, TCount>(
            this IHybridEstimatorFullData<int, TCount> estimatorData,
            IBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            uint factor)
            where TCount : struct
            where TId : struct
        {
            if (estimatorData == null) return null;
            var minWiseFold = estimatorData.BitMinwiseEstimator == null ? 1L :
                Math.Max(1L, (MathExtensions.GetFactors(estimatorData.BitMinwiseEstimator.Capacity).OrderBy(f => f).FirstOrDefault(f => f > factor)));
            return new HybridEstimatorFullData<int, TCount>
            {
                ItemCount = estimatorData.ItemCount,
                BlockSize = estimatorData.BlockSize / factor,
                StrataCount = estimatorData.StrataCount,
                BitMinwiseEstimator = estimatorData.BitMinwiseEstimator?.Fold((uint)minWiseFold),
                StrataEstimator =
                    estimatorData.StrataEstimator?.Fold(configuration.ConvertToEstimatorConfiguration(), factor)
            };
        }

        /// <summary>
        /// Compress the hybrid estimator.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="estimatorData"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static HybridEstimatorFullData<int, TCount> Compress<TEntity, TId, TCount>(
           this IHybridEstimatorFullData<int, TCount> estimatorData,
           IBloomFilterConfiguration<TEntity, TId, int, TCount> configuration)
           where TCount : struct
           where TId : struct
        {
            if (configuration?.FoldingStrategy == null || estimatorData == null) return null;
            var fold = configuration.FoldingStrategy.FindFoldFactor(
                estimatorData.BlockSize,
                estimatorData.BlockSize,
                estimatorData.ItemCount);
            var res = fold.HasValue ? estimatorData.Fold(configuration, fold.Value) : null;
            return res;
        }

        /// <summary>
        /// Convert full data to serializable data.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="estimatorData"></param>
        /// <returns></returns>
        public static IHybridEstimatorData<TId, TCount> ToEstimatorData<TId,TCount>(
            this HybridEstimatorFullData<TId, TCount> estimatorData)
            where TCount : struct
            where TId : struct
        {
            if (estimatorData == null) return null;
            return new HybridEstimatorData<TId, TCount>
            {
                BlockSize = estimatorData.BlockSize,
                ItemCount = estimatorData.ItemCount,
                StrataCount = estimatorData.StrataCount,
                StrataEstimator = estimatorData.StrataEstimator,
                BitMinwiseEstimator = estimatorData.BitMinwiseEstimator == null ?
                    null :
                    new BitMinwiseHashEstimatorData
                    {
                        BitSize = estimatorData.BitMinwiseEstimator.BitSize,
                        Capacity = estimatorData.BitMinwiseEstimator.Capacity,
                        HashCount = estimatorData.BitMinwiseEstimator.HashCount,
                        ItemCount = estimatorData.BitMinwiseEstimator.ItemCount,
                        Values = estimatorData.BitMinwiseEstimator.Values.ConvertToBitArray(estimatorData.BitMinwiseEstimator.BitSize).ToBytes()
                    }
            };
        }
    }
}
