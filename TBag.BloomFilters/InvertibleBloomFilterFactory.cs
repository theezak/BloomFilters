namespace TBag.BloomFilters
{
    using System;
     using Estimators;

    /// <summary>
    /// Place holder for a factory to create Bloom filters based upon strata estimators.
    /// </summary>
    public class InvertibleBloomFilterFactory : IInvertibleBloomFilterFactory
    {
        /// <summary>
        /// Create an invertible Bloom filter for high utilization (many more items added than it was sized for).
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="bloomFilterConfiguration"></param>
        /// <param name="capacity"></param>
        /// <param name="errorRate"></param>
        /// <returns></returns>
        public IInvertibleBloomFilter<TEntity, TId, int> CreateHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity,int,TId,long,int> bloomFilterConfiguration,
            long capacity,
            float? errorRate = null)
        {
            if (errorRate.HasValue)
            {
                return new InvertibleBloomFilter<TEntity, TId, int>(capacity, errorRate.Value, bloomFilterConfiguration);
            }
            return new InvertibleBloomFilter<TEntity, TId, int>(capacity, bloomFilterConfiguration);
        }

        /// <summary>
        /// Create an invertible Bloom filter for high utilization (many more items added than it was sized for).
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="bloomFilterConfiguration"></param>
        /// <param name="estimator"></param>
        /// <param name="otherEstimator"></param>
        /// <param name="errorRate"></param>
        /// <param name="destructive"></param>
        /// <returns></returns>
        public IInvertibleBloomFilter<TEntity, TId, int> CreateHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, int, TId, long, int> bloomFilterConfiguration,
            IHybridEstimatorData<TId, int> estimator,
            IHybridEstimatorData<TId, int> otherEstimator,
            float? errorRate = null,
            bool destructive = false)
        {
            var estimate = estimator.Decode(otherEstimator, bloomFilterConfiguration, destructive);
            //slight cheat, but knowing the size of both sets gives a real nice upperbound for the maximum number of differences.
            var capacity = Math.Max(15L,
                Math.Min(1.2D*(estimator.CountEstimate + otherEstimator.CountEstimate), (long)(Math.Pow(1.4D, Math.Log(estimator.CountEstimate + otherEstimator.CountEstimate) -1)*estimate*estimate)));
            return CreateHighUtilizationFilter(
                bloomFilterConfiguration,
                (long) capacity,
                errorRate ?? 0.001F);
        }

        /// <summary>
        /// Create an invertible Bloom filter that is compatible with the given bloom filter data.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="bloomFilterConfiguration"></param>
        /// <param name="capacity"></param>
        /// <param name="invertibleBloomFilterData"></param>
        /// <returns></returns>
        /// <remarks>For the scenario where you need to match a received filter with the set you own, so you can find the differences.</remarks>
        public IInvertibleBloomFilter<TEntity, TId, int> CreateMatchingHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, int, TId, long, int> bloomFilterConfiguration,
            long capacity,
           IInvertibleBloomFilterData<TId, int> invertibleBloomFilterData)
        {
            var blockSize = invertibleBloomFilterData.BlockSize;          
            return new InvertibleBloomFilter<TEntity, TId, int>(capacity, bloomFilterConfiguration, blockSize, invertibleBloomFilterData.HashFunctionCount);
        }

        /// <summary>
        /// Create an invertible Bloom filter
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="bloomFilterConfiguration"></param>
        /// <param name="capacity"></param>
        /// <param name="errorRate"></param>
        /// <returns></returns>
        /// <remarks>Assumption is that the utilization will be in line with the capacity, thus keeping individual counts low.</remarks>
        public IInvertibleBloomFilter<TEntity, TId, sbyte> Create<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, int, TId, long, sbyte> bloomFilterConfiguration,
            long capacity,
            float? errorRate = null)
        {
            return errorRate.HasValue ? 
                new InvertibleBloomFilter<TEntity, TId, sbyte>(capacity, errorRate.Value, bloomFilterConfiguration) : 
                new InvertibleBloomFilter<TEntity, TId, sbyte>(capacity, bloomFilterConfiguration);
        }
    }
}
