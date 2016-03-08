namespace TBag.BloomFilters
{
    public interface IStrataEstimatorData<TId,TCount>
    {
        IInvertibleBloomFilterData<TId,TCount>[] BloomFilters { get; set; }
        long Capacity { get; set; }
    }
}