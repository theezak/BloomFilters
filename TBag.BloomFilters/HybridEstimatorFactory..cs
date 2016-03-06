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
        /// <param name="failedDecode">Number of times decoding has failed based upon the provided estimator.</param>
        /// <returns></returns>
        public HybridEstimator<T,TId> Create<T,TId>(
            IBloomFilterConfiguration<T,int,TId,long> configuration, 
            ulong setSize, 
            byte failedDecodeCount = 0)
        {
            byte strata = 7;
            ulong capacity = 15;
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
            return new HybridEstimator<T, TId>(capacity, 2, 40, setSize, strata, configuration);
        }
    }
}
