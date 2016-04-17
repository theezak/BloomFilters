namespace TBag.BloomFilters.Invertible.Configurations
{
    using System.Collections.Generic;
    using System;
    using Invertible;
    using BloomFilters.Configurations;

    /// <summary>
    /// Interface for configuration of a Bloom filter.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The identifier type</typeparam>
    /// <typeparam name="THash">The hash value type</typeparam>
    /// <typeparam name="TCount">The occurence count type.</typeparam>
    /// <remarks>Not the most efficient or elegant implementation, but useful for a test bed.</remarks>
    public interface IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> : 
        ICountingBloomFilterConfiguration<TId, THash, TCount>
        where THash : struct
        where TCount : struct
        where TId : struct
    {

        /// <summary>
        /// Function to create a value hash for a given entity.
        /// </summary>
        Func<TEntity, THash> EntityHash { get; set; }

        /// <summary>
        /// Perform a XOR between identifiers.
        /// </summary>
        Func<TId, TId, TId> IdAdd { get; set; }

        /// <summary>
        /// Perform a XOR between identifiers.
        /// </summary>
        Func<TId, TId, TId> IdRemove { get; set; }

        /// <summary>
        /// Perform a AND between identifiers.
        /// </summary>
        Func<TId, TId, TId> IdIntersect { get; set; }

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
        EqualityComparer<TId> IdEqualityComparer { get; set; }

        /// <summary>
        /// The identity value for <typeparamref name="TId"/> (for example 0 when the identifier is a number).
        /// </summary>
        TId IdIdentity { get; set; }

        /// <summary>
        /// The identity value for <typeparamref name="THash"/> (for example 0 when the identifier is a number).
        /// </summary>
        THash HashIdentity { get; set; }

        /// <summary>
        /// Function to get the identifier for a given entity.
        /// </summary>
        Func<TEntity, TId> GetId { get; set; }

        /// <summary>
        /// Configuration for the Bloom filter that hashes values.
        /// </summary>
        /// <remarks>Only used by a hybrid IBF that utilizes both an IBF and a reverse IBF (this being the data for the reverse IBF)</remarks>
        IInvertibleBloomFilterConfiguration<KeyValuePair<TId, THash>, TId, THash, TCount> SubFilterConfiguration { get; }

        /// <summary>
        /// Determine if the location in the given data is pure.
        /// </summary>
        Func<IInvertibleBloomFilterData<TId, THash, TCount>, long, bool> IsPure { get; set; }

        /// <summary>
        /// Data factory
        /// </summary>
        IInvertibleBloomFilterDataFactory DataFactory { get; }
    }
}
