

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
            var idealBlockSize =  filter.Configuration.BestCompressedSize(
                filter.ItemCount,
                filter.ErrorRate);
            var idealErrorRate = filter.Configuration.ActualErrorRate(
                idealBlockSize,
                filter.ItemCount,
                filter.HashFunctionCount);
            var actualErrorRate = Math.Max(
                idealErrorRate,
                filter.Configuration.ActualErrorRate(
                    filter.BlockSize,
                    filter.ItemCount,
                    filter.HashFunctionCount));
            var factor = (actualErrorRate - idealErrorRate);
            if (actualErrorRate >= 0.9D &&
                filter.BlockSize > 0)
            {
                //arbitrary. Should really figure out what is behind this one day : - ). What happens is that the estimator has an extremely high
                //false-positive rate. Which is the reason why this approach is not ideal to begin with. 
                factor = 2 * factor * ((float)idealBlockSize / filter.BlockSize);
            }
            return QuasiEstimator.Decode(
                filter.ItemCount,
                idealErrorRate,
                filter.Contains,
                otherSetSample,
                otherSetSize,
                (membershipCount, sampleCount) => (long)Math.Floor(membershipCount - factor * ((otherSetSize ?? sampleCount) - membershipCount)));
        }
    }
}
