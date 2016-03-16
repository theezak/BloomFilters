namespace TBag.BloomFilters
{
    /// <summary>
    /// interface for invertible Bloom filter data.
    /// </summary>
    /// <typeparam name="TId"></typeparam>  
    /// <typeparam name="TEntityHash"></typeparam>
    /// <typeparam name="TCount">The type for the count</typeparam>
    public interface IInvertibleBloomFilterData<TId,TEntityHash,TCount>
        where TCount : struct
        where TEntityHash : struct
        where TId : struct
    {
        /// <summary>
        /// <c>true</c> when the identifier and hash have been reversed, else <c>false</c>.
        /// </summary>
        bool IsReverse { get; set; }

        /// <summary>
        /// The block size 
        /// </summary>
        /// <remarks>Is the length of the arrays with hashes and counts, unless the Bloom filter was split by hash function, in which case the block size times the number of hash functions equals the size of the arrays.</remarks>
        long BlockSize { get; set; }

        /// <summary>
        /// The counts
        /// </summary>
        TCount[] Counts { get; set; }

        /// <summary>
        /// The number of hash functions used.
        /// </summary>
        uint HashFunctionCount { get; set; }

        /// <summary>
        /// The hash sums (for entity values).
        /// </summary>
        TEntityHash[] HashSums { get; set; }

        /// <summary>
        /// The identifier sums (for entity identifiers).
        /// </summary>
        TId[] IdSums { get; set; }

        /// <summary>
        /// The Bloom filter data for the value hash (optional).
        /// </summary>
        InvertibleBloomFilterData<TEntityHash, TId, TCount> ReverseFilter { get; set; }
    }
}