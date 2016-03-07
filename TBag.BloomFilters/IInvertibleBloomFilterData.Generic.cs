namespace TBag.BloomFilters
{
    public interface IInvertibleBloomFilterData<TId>
    {
        long BlockSize { get; set; }
        int[] Counts { get; set; }
        uint HashFunctionCount { get; set; }
        int[] HashSums { get; set; }
        TId[] IdSums { get; set; }
    }
}