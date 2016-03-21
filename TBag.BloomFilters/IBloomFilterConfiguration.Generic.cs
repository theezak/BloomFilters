namespace TBag.BloomFilters
{
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// Interface for configuration of a Bloom filter.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The identifier type</typeparam>
    /// <typeparam name="THash">The hash value type</typeparam>
    /// <typeparam name="TCount">The occurence count type.</typeparam>
    /// <remarks>Not the most efficient or elegant implementation, but useful for a test bed.</remarks>
    public interface IBloomFilterConfiguration<TEntity, TId, THash, TCount> :
        IBloomFilterSizeConfiguration
        where THash : struct
        where TCount : struct
        where TId : struct
    {
        /// <summary>
        /// Count configuration.
        /// </summary>
        ICountConfiguration<TCount> CountConfiguration { get; set; }

        /// <summary>
        /// The equality comparer for <typeparamref name="THash"/>.
        /// </summary>
        EqualityComparer<THash> HashEqualityComparer { get; set; }
        
        /// <summary>
        /// Configuration for the Bloom filter that hashes values.
        /// </summary>
        /// <remarks>Only used by a hybrid IBF that utilizes both an IBF and a reverse IBF (this being the data for the reverse IBF)</remarks>
        IBloomFilterConfiguration<KeyValuePair<TId,THash>, TId, THash, TCount> ValueFilterConfiguration { get;  }

        /// <summary>
        /// Function to create a sequence of given length of hashes.
        /// </summary>
        Func<THash, THash, uint, IEnumerable<THash>> Hashes { get; set; }

        /// <summary>
        /// Determine if the location in the given data is pure.
        /// </summary>
        Func<IInvertibleBloomFilterData<TId, THash, TCount>, long,  bool> IsPure { get; set; }

        /// <summary>
        /// Function to create a value hash for a given entity.
        /// </summary>
        Func<TEntity, THash> EntityHash { get; set; }

        /// Function to create an identifier hash for a given entity.
        /// </summary>
        Func<TId, THash> IdHash { get; set; }

        /// <summary>
        /// Perform a XOR between identifiers.
        /// </summary>
        Func<TId, TId, TId> IdXor { get; set; }

        /// <summary>
        /// Hash XOR
        /// </summary>
        Func<THash, THash, THash> HashXor { get; set; }

        /// <summary>
        /// The identity value for <typeparamref name="TId"/> (for example 0 when the identifier is a number).
        /// </summary>
        Func<TId> IdIdentity { get; set; }

        /// <summary>
        /// The identity value for <typeparamref name="THash"/> (for example 0 when the identifier is a number).
        /// </summary>
        Func<THash> HashIdentity { get; set; }

        /// <summary>
        /// Function to get the identifier for a given entity.
        /// </summary>
        Func<TEntity, TId> GetId { get; set; }

        /// <summary>
        /// Equality comparer for <typeparamref name="TId"/>.
        /// </summary>
        EqualityComparer<TId> IdEqualityComparer { get; set; }

        IInvertibleBloomFilterDataFactory DataFactory { get; }

        /// <summary>
        /// Determine if the configuration supports the given capacity and set size.
        /// </summary>
        /// <param name="capacity">Capacity for the Bloom filter</param>
        /// <param name="size">The actual set size.</param>
        /// <returns></returns>
        bool Supports(long capacity, long size);
    }

}
