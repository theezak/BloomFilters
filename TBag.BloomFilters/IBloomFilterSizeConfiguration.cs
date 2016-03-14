namespace TBag.BloomFilters
{
    public interface IBloomFilterSizeConfiguration
    {
        uint BestHashFunctionCount(long capacity, float errorRate);

        long BestCompressedSize(long capacity, float errorRate);

        long BestSize(long capacity, float errorRate);

        float BestErrorRate(long capacity);
    }
}
