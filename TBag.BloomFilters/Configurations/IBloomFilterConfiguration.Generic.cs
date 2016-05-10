namespace TBag.BloomFilters.Configurations
{
    using System;
    using System.Collections.Generic;


    /// <summary>
    /// Configuration for a Bloom filter
    /// </summary>
    /// <typeparam name="TKey">The type of the key</typeparam>
    /// <typeparam name="THash">The type of the hash</typeparam>
    public interface IBloomFilterConfiguration<TKey, THash> : 
        IBloomFilterSizeConfiguration
     where TKey : struct
     where THash : struct
    {
        /// <summary>
        /// The equality comparer for <typeparamref name="THash"/>.
        /// </summary>
        EqualityComparer<THash> HashEqualityComparer { get; set; }

        /// <summary>
        /// Function to create a sequence of given length of hashes.
        /// </summary>
        Func<THash, uint, IEnumerable<THash>> Hashes { get; set; }

        /// <summary>
        /// Function to create an identifier hash for a given entity.
        /// </summary>
        Func<TKey, THash> IdHash { get; set; }

        /// <summary>
        /// Determine if the configuration supports the given capacity and set size.
        /// </summary>
        /// <param name="capacity">Capacity for the Bloom filter</param>
        /// <param name="size">The actual set size.</param>
        /// <returns></returns>
        bool Supports(long capacity, long size);

        /// <summary>
        /// Strategy for folding Bloom filters.
        /// </summary>
        IFoldingStrategy FoldingStrategy { get; set; }


        /// <summary>
        /// Perform a XOR between identifiers.
        /// </summary>
        Func<TKey, TKey, TKey> IdAdd { get; set; }

        /// <summary>
        /// Perform a XOR between identifiers.
        /// </summary>
        Func<TKey, TKey, TKey> IdRemove { get; set; }

        /// <summary>
        /// Perform a AND between identifiers.
        /// </summary>
        Func<TKey, TKey, TKey> IdIntersect { get; set; }

        /// <summary>
        /// Hash XOR
        /// </summary>
        Func<THash, THash, THash> HashAdd { get; set; }

        /// <summary>
        /// Hash XOR
        /// </summary>
        Func<THash, THash, THash> HashRemove { get; set; }

        /// <summary>
        /// Hash AND
        /// </summary>
        Func<THash, THash, THash> HashIntersect { get; set; }

        /// <summary>
        /// Equality comparer for <typeparamref name="TId"/>.
        /// </summary>
        EqualityComparer<TKey> IdEqualityComparer { get; set; }

        /// <summary>
        /// The identity value for <typeparamref name="TId"/> (for example 0 when the identifier is a number).
        /// </summary>
        TKey IdIdentity { get; set; }

        /// <summary>
        /// The identity value for <typeparamref name="THash"/> (for example 0 when the identifier is a number).
        /// </summary>
        THash HashIdentity { get; set; }
    }

}
