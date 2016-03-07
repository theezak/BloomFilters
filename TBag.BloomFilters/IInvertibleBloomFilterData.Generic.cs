namespace TBag.BloomFilters
{
    /// <summary>
    /// interface for invertible Bloom filter data.
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    public interface IInvertibleBloomFilterData<TId>
    {
        /// <summary>
        /// The block size 
        /// </summary>
        /// <remarks>Is the length of the arrays with hashes and counts, unless the Bloom filter was split by hash function, in which case the block size times the number of hash functions equals the size of the arrays.</remarks>
        long BlockSize { get; set; }

        /// <summary>
        /// The counts
        /// </summary>
        int[] Counts { get; set; }

        /// <summary>
        /// The number of hash functions used.
        /// </summary>
        uint HashFunctionCount { get; set; }

        /// <summary>
        /// The hash sums (for entity values).
        /// </summary>
        int[] HashSums { get; set; }

        /// <summary>
        /// The identifier sums (for entity identifiers).
        /// </summary>
        TId[] IdSums { get; set; }
    }
}