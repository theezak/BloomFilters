namespace TBag.BloomFilters
{
    public interface IInvertibleBloomFilterFactory
    {
        IInvertibleBloomFilter<TEntity, TId, int> CreateHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity,int,TId,long,int> bloomFilterConfiguration,
            long capacity,
            float? errorRate = null);

        IInvertibleBloomFilter<TEntity, TId, int> CreateMatchingHighUtilizationFilter<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, int, TId, long, int> bloomFilterConfiguration,
            long capacity,
            IInvertibleBloomFilterData<TId, int> invertibleBloomFilterData);

        IInvertibleBloomFilter<TEntity, TId, sbyte> Create<TEntity, TId>(
            IBloomFilterConfiguration<TEntity, int, TId, long, sbyte> bloomFilterConfiguration,
            long capacity,
            float? errorRate = null);
    }
}