namespace TBag.BloomFilters
{
    public interface IInvertibleBloomFilterDataFactory
    {
        InvertibleBloomFilterData<TId, THash, TCount> Create<TId, THash, TCount>(long m, uint k)
            where TId : struct
            where TCount : struct
            where THash : struct;
    }
}