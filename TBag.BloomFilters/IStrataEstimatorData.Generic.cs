namespace TBag.BloomFilters
{
    public interface IStrataEstimatorData<TId>
    {
        IInvertibleBloomFilterData<TId>[] BloomFilters { get; set; }
        ulong Capacity { get; set; }
    }
}