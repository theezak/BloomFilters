
namespace TBag.BloomFilters.Countable
{
    /// <summary>
    /// Interface for a counting Bloom filter
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TCount">Count type</typeparam>
    interface ICountingBloomFilter<TEntity, TKey, TCount> 
       where TKey : struct
        where TCount : struct
    {
    }
}
