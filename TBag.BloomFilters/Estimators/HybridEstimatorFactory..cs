namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Encapsulates emperical data for creating hybrid estimators.
    /// </summary>
    public class HybridEstimatorFactory : IHybridEstimatorFactory
    {
        /// <summary>
        /// Create a hybrid estimator
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <typeparam name="TCount">The type of occurence count.</typeparam>
        /// <param name="configuration">Bloom filter configuration</param>
        /// <param name="setSize">Number of elements in the set that is added.</param>
        /// <param name="failedDecodeCount">Number of times decoding has failed based upon the provided estimator.</param>
        /// <returns></returns>
        public HybridEstimator<TEntity,TId,TCount> Create<TEntity,TId,TCount>(
            IBloomFilterConfiguration<TEntity,int,TId,long,TCount> configuration, 
            ulong setSize, 
            byte failedDecodeCount = 0)
            where TCount : struct
        {
            byte strata = 7;
            var capacity = 15L;
            if (setSize < 10000L && failedDecodeCount >0)
            {
                capacity = capacity * failedDecodeCount * 10;
                if (failedDecodeCount >= 4 && failedDecodeCount <= 6)
                {
                    //higher strata is typically not preferred with lower capacities, but try it at this point.
                    strata = 13;
                }
            }
            if (setSize >= 10000L)
            {
                capacity = 1000;
                if (failedDecodeCount > 0)
                {
                    strata = 13;
                }
            }
            if (setSize >= 1000000L)
            {
                strata = 13;
                if (failedDecodeCount > 0)
                {
                    strata = 19;
                }
            }
            return new HybridEstimator<TEntity, TId, TCount>(capacity, 2, 30, setSize, strata, configuration);
        }
    }
}
