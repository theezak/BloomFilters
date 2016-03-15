namespace TBag.BloomFilters
{
    /// <summary>
    /// Interface for Bloom filter size configuration
    /// </summary>
    public interface IBloomFilterSizeConfiguration
    {
        /// <summary>
        /// Determine the best number of hash functions based upon <paramref name="capacity"/> and desired <paramref name="errorRate"/>.
        /// </summary>
        /// <param name="capacity">The capacity for the Bloom filter</param>
        /// <param name="errorRate">The desired error rate (value between 0 and 1)</param>
        /// <returns>The recommended number of hash functions for the IBF</returns>
        uint BestHashFunctionCount(long capacity, float errorRate);

        /// <summary>
        /// Calculate the best compressed size based upon <paramref name="capacity"/> and desired <paramref name="errorRate"/>.
        /// </summary>
        /// <param name="capacity">The capacity for the Bloom filter</param>
        /// <param name="errorRate">The desired error rate (value between 0 and 1)</param>
        /// <returns></returns>
        long BestCompressedSize(long capacity, float errorRate);

        /// <summary>
        /// Calculate the best size (uncompressed) based upon <paramref name="capacity"/> and desired <paramref name="errorRate"/>.
        /// </summary>
        /// <param name="capacity">The capacity for the Bloom filter</param>
        /// <param name="errorRate">The desired error rate (value between 0 and 1)</param>
        /// <returns></returns>
        long BestSize(long capacity, float errorRate);

        /// <summary>
        /// Calculate the best error rate given the capacity.
        /// </summary>
        /// <param name="capacity">The capacity for the Bloom filter</param>
        /// <returns></returns>
        /// <remarks>This is based upon a heuristic, in general you are best off to provide a specific error rate.</remarks>
        float BestErrorRate(long capacity);
    }
}
