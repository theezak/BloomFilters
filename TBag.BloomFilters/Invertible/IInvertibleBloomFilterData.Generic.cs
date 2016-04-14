using TBag.BloomFilters.Configurations;
using TBag.BloomFilters.Invertible.Configurations;

namespace TBag.BloomFilters.Invertible
{
    /// <summary>
    /// interface for invertible Bloom filter data.
    /// </summary>
    /// <typeparam name="TId">The entity identifier type</typeparam>  
    /// <typeparam name="THash">The hash type</typeparam>
    /// <typeparam name="TCount">The type for the count</typeparam>
    public interface IInvertibleBloomFilterData<TId, THash, TCount>
        where TCount : struct
        where THash : struct
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
        THash[] HashSums { get; set; }

        /// <summary>
        /// The identifier sums (for entity identifiers).
        /// </summary>
        TId[] IdSums { get; set; }

        /// <summary>
        /// The Bloom filter data for the value hash (optional).
        /// </summary>
        InvertibleBloomFilterData<TId, THash, TCount> SubFilter { get; set; }

        /// <summary>
        /// Number of items in the Bloom filter.
        /// </summary>
        long ItemCount { get; set; }

        /// <summary>
        /// The capacity
        /// </summary>
        long Capacity { get; set; }

        /// <summary>
        /// The hashSum provider.
        /// </summary>
        ICompressedArray<THash> HashSumProvider { get; }

        /// <summary>
        /// The IdSum provider.
        /// </summary>
        ICompressedArray<TId> IdSumProvider { get; }

        /// <summary>
        /// Set the compression providers.
        /// </summary>
        /// <param name="configuration"></param>
        void SyncCompressionProviders(
            ICountingBloomFilterConfiguration<TId, THash, TCount> configuration);
    }
}