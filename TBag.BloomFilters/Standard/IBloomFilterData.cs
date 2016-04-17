namespace TBag.BloomFilters.Standard
{
    internal interface IBloomFilterData
    {
        long BlockSize { get; set; }
        long Capacity { get; set; }
        uint HashFunctionCount { get; set; }
        long ItemCount { get; set; }
        byte[] Bits { get; set; }
    }
}