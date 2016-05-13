using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TBag.BloomFilters.Estimators;

namespace TBag.BloomFilters.Standard
{
    public static class BloomFilterExtensions
    {
        /// <summary>
        /// Quasi decode a given <paramref name="filter">filter</paramref>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The identifier type</typeparam>
        /// <param name="filter">The Bloom filter</param>
        /// <param name="otherSetSample"></param>
        /// <param name="otherSetSize"></param>
        /// <returns></returns>
        public static long? QuasiDecode<TEntity, TId>(
            this IBloomFilter<TEntity, TId> filter,
             IEnumerable<TEntity> otherSetSample,
            long? otherSetSize = null)
            where TId : struct
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
