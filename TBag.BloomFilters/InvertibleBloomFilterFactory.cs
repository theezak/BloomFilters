using System.Runtime.Remoting;

namespace TBag.BloomFilters
{
    /// <summary>
    /// Place holder for a factory to create Bloom filters based upon strata estimators.
    /// </summary>
    public class InvertibleBloomFilterFactory : IInvertibleBloomFilterFactory
    {
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

        public IInvertibleBloomFilter<TEntity, TId, int> CreateMatchingHighUtilizationFilter<TEntity, TId>(IBloomFilterConfiguration<TEntity, int, TId, long, int> bloomFilterConfiguration,
            long capacity,
           IInvertibleBloomFilterData<TId, int> invertibleBloomFilterData)
        {
            var blockSize = invertibleBloomFilterData.BlockSize;          
            return new InvertibleBloomFilter<TEntity, TId, int>(capacity, bloomFilterConfiguration, blockSize, invertibleBloomFilterData.HashFunctionCount);
        }

        public IInvertibleBloomFilter<TEntity, TId, sbyte> Create<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, int, TId, long, sbyte> bloomFilterConfiguration,
            long capacity,
            float? errorRate = null)
        {
            if (errorRate.HasValue)
            {
                return new InvertibleBloomFilter<TEntity, TId, sbyte>(capacity, errorRate.Value, bloomFilterConfiguration);
            }
            return new InvertibleBloomFilter<TEntity, TId, sbyte>(capacity, bloomFilterConfiguration);
        }
    }
}
