using TBag.BloomFilters.Configurations;

namespace TBag.BloomFilters.Countable.Configurations
{
    public interface IEntityCountingBloomFilterConfiguration<TEntity,TKey,THash,TCount> : 
        ICountingBloomFilterConfiguration<TKey,THash,TCount>,
        IEntityBloomFilterConfiguration<TEntity,TKey,THash>
        where TKey :struct
        where THash : struct
        where TCount : struct
    {
    }
}
