namespace TBag.BloomFilters.Configurations
{
     /// <summary>
    /// Interface for configuration of a Bloom filter.
    /// </summary>
     /// <typeparam name="TKey">The key type</typeparam>
    /// <typeparam name="THash">The hash value type</typeparam>
    /// <typeparam name="TCount">The occurence count type.</typeparam>
    public interface ICountingBloomFilterConfiguration<TKey, THash, TCount> :
        IBloomFilterConfiguration<TKey, THash>
        where THash : struct
        where TCount : struct
        where TKey : struct
    {
        /// <summary>
        /// Factory for creating compressed arrays
        /// </summary>
        ICompressedArrayFactory CompressedArrayFactory { get; set; }

        /// <summary>
        /// Count configuration.
        /// </summary>
        ICountConfiguration<TCount> CountConfiguration { get; set; }
    }
}
