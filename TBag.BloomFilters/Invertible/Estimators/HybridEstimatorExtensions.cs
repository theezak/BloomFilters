namespace TBag.BloomFilters.Invertible.Estimators
{
    using System.Collections.Generic;
    using System.Linq;
    using BloomFilters.Configurations;
    using System;
    using BloomFilters.Estimators;

    public static class HybridEstimatorExtensions
    {
        /// <summary>
        /// Approximate the size of the set difference based upon an estimator and a (subset) of the other set
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="estimator">The hybrid estimator</param>
        /// <param name="bloomFilterSizeConfiguration"></param>
        /// <param name="otherSetSample">A (sub)set to compare against</param>
        /// <param name="otherSetSize">Total set of the size to compare against (when not given, the set sample size is used)</param>
        /// <returns>An estimate for the number of differences, or <c>null</c> when a reasonable estimate is not possible.</returns>
        /// <remarks>Not an ideal solution, due to the potentially high false positive rate of the estimator. But useful when you do not have a local estimator, but you do have a (sub)set of the data and know the total size of the data. Known issue is that small differences on very large sets are either grossly over estimated (when there is a count difference between the two sets) or not recognized at all (under estimated, when both sets have the same count, but different values). The estimate can be rather inexact. See 'Exact Set Reconciliation Based on Bloom Filters', Dafang Zhang, Kun Xie, 2011 International Conference on Computer Science and Network Technology, page 2001-2009 </remarks>
        public static long? QuasiDecode<TEntity, TId, TCount>(
            this IHybridEstimator<TEntity, TId, TCount> estimator,
            IBloomFilterSizeConfiguration bloomFilterSizeConfiguration,
            IEnumerable<TEntity> otherSetSample,
            long? otherSetSize = null)
            where TId : struct
            where TCount : struct
        {
            if (estimator == null) return otherSetSize ?? otherSetSample?.LongCount() ?? 0L;
            //compensate for extremely high error rates that can occur with estimators. Without this, the difference goes to infinity.
            var factors = QuasiEstimator.GetAdjustmentFactor(
                bloomFilterSizeConfiguration,
                estimator.VirtualBlockSize, 
                estimator.ItemCount, 
                estimator.HashFunctionCount, 
                estimator.ErrorRate);
            return QuasiEstimator.Decode(
                estimator.ItemCount, 
                factors.Item1, 
                estimator.Contains, 
                otherSetSample,
                otherSetSize,
                factors.Item2);                                 
        }
    }
}
