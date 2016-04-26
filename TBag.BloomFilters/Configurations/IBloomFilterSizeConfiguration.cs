namespace TBag.BloomFilters.Configurations
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
        /// Calculate the best hash function count given capacity and block size.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="blockSize"></param>
        /// <returns></returns>
        uint BestHashFunctionCount(long capacity, long blockSize);

        /// <summary>
        /// Calculate the best compressed size based upon <paramref name="capacity"/> and desired <paramref name="errorRate"/>.
        /// </summary>
        /// <param name="capacity">The capacity for the Bloom filter</param>
        /// <param name="errorRate">The desired error rate (value between 0 and 1)</param>
        /// <param name="foldFactor">The desired fold factor.</param>
        /// <returns></returns>
        long BestCompressedSize(long capacity, float errorRate, int foldFactor = 0);

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

        /// <summary>
        /// Calculate the actual error rate.
        /// </summary>
        /// <param name="blockSize"></param>
        /// <param name="itemCount"></param>
        /// <param name="hashFunctionCount"></param>
        /// <returns></returns>
        float ActualErrorRate(long blockSize, long itemCount, uint hashFunctionCount);

        /// <summary>
        /// The minimum number of hash function counts to use.
        /// </summary>
        uint MinimumHashFunctionCount { get; set; }
    }
}
