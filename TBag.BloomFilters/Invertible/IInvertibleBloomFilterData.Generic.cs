

namespace TBag.BloomFilters.Invertible
{
    using Countable.Configurations;
    using System;
    using TBag.BloomFilters.Invertible.Configurations;

    /// <summary>
    /// interface for invertible Bloom filter data.
    /// </summary>
    /// <typeparam name="TId">The entity identifier type</typeparam>  
    /// <typeparam name="THash">The hash type</typeparam>
    /// <typeparam name="TCount">The type for the count</typeparam>
    public interface IInvertibleBloomFilterData<TId, THash, TCount> : IBloomFilterMetadata
        where TCount : struct
        where THash : struct
        where TId : struct
    {
        /// <summary>
        /// <c>true</c> when the identifier and hash have been reversed, else <c>false</c>.
        /// </summary>
        bool IsReverse { get; set; }

        /// <summary>
        /// The counts
        /// </summary>
        TCount[] Counts { get; set; }

        /// <summary>
        /// The error rate
        /// </summary>
        float ErrorRate { get; set; }

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

        /// <summary>
        /// Clear the Bloom filter data
        /// </summary>
        void Clear<TEntity>(IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration);

        void ExecuteExclusively(long lockPosition, Action action);
    }
}