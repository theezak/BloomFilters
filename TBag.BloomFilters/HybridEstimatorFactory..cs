namespace TBag.BloomFilters
{
    /// <summary>
    /// Encapsulates emperical data for creating hybrid estimators.
    /// </summary>
    public class HybridEstimatorFactory
    {
        /// <summary>
        /// Create a hybrid estimator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="configuration">Bloom filter configuration</param>
        /// <param name="setSize">Number of elements in the set that is added.</param>
        /// <param name="failedDecodeCount">Number of times decoding has failed based upon the provided estimator.</param>
        /// <returns></returns>
        public HybridEstimator<T,TId,TCount> Create<T,TId,TCount>(
            IBloomFilterConfiguration<T,int,TId,long,TCount> configuration, 
            ulong setSize, 
            byte failedDecodeCount = 0)
            where TCount : struct
        {
            byte strata = 7;
            var capacity = 15L;
            if (setSize < 10000L && failedDecodeCount >0)
            {
                capacity = capacity * failedDecodeCount * 10;
                if (failedDecodeCount >= 2 && failedDecodeCount <= 4)
                {
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
            return new HybridEstimator<T, TId, TCount>(capacity, 2, 40, setSize, strata, configuration);
        }
    }
}
