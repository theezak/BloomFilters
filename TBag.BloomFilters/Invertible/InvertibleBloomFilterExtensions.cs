

namespace TBag.BloomFilters.Invertible
{
    using BloomFilters.Configurations;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TBag.BloomFilters.Estimators;

    /// <summary>
    /// Extension methods for invertible Bloom filter.
    /// </summary>
    public static class InvertibleBloomFilterExtensions
    {
        /// <summary>
        /// Quasi decode a given <paramref name="filter">filter</paramref>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The identifier type</typeparam>
        /// <typeparam name="TCount">The count type</typeparam>
        /// <param name="filter">The Bloom filter</param>
        /// <param name="otherSetSample"></param>
        /// <param name="otherSetSize"></param>
        /// <returns></returns>
        public static long? QuasiDecode<TEntity, TId, TCount>(
            this IInvertibleBloomFilter<TEntity, TId, TCount> filter,
             IEnumerable<TEntity> otherSetSample,
            long? otherSetSize = null)
            where TId : struct
            where TCount : struct
        {
            if (filter == null) return otherSetSize ?? otherSetSample?.LongCount() ?? 0L;
            //compensate for extremely high error rates that can occur with estimators. Without this, the difference goes to infinity.
            var factor = QuasiEstimator.GetAdjustmentFactor(filter.Configuration, filter.BlockSize, filter.ItemCount, filter.HashFunctionCount, filter.ErrorRate);
            return QuasiEstimator.Decode(
                filter.ItemCount,
               factor.Item1,
                filter.Contains,
                otherSetSample,
                otherSetSize,
                factor.Item2);
        }
    }
}
