using System;

namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Extension methods for the hybrid estimator data.
    /// </summary>
    public static class HybridEstimatorDataExtensions
    {       
        public static ulong Decode<TEntity,TId,TCount>(this IHybridEstimatorData<TId,TCount> estimator,
            IHybridEstimatorData<TId,TCount> otherEstimatorData,
            IBloomFilterConfiguration<TEntity, int, TId, long, TCount> configuration)
            where TCount : struct
        {
            if (estimator == null && otherEstimatorData == null) return 0L;
            if (estimator == null) return (ulong)otherEstimatorData.Capacity;
            if (otherEstimatorData == null) return (ulong)estimator.Capacity;
            var decodeFactor = Math.Max(estimator.StrataEstimator?.DecodeCountFactor ?? 1.0D, 
                otherEstimatorData.StrataEstimator?.DecodeCountFactor ?? 1.0D);
            return estimator.StrataEstimator.Decode(otherEstimatorData.StrataEstimator, configuration) + 
               (ulong)(decodeFactor*(estimator.BitMinwiseEstimator.Capacity - (estimator.BitMinwiseEstimator.Similarity(otherEstimatorData.BitMinwiseEstimator) * estimator.BitMinwiseEstimator.Capacity)));

        }
    }
}
