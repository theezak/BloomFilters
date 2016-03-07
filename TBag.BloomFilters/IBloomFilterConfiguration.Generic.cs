using System;
namespace TBag.BloomFilters
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface for configuration of a Bloom filter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="THash"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TIdHash"></typeparam>
    public interface IBloomFilterConfiguration<T, THash, TId, TIdHash>
       where THash : struct
      where TIdHash : struct
    {
        /// <summary>
        /// Function to get the value of an entity (hashed).
        /// </summary>
        /// <remarks>Any of the data that contributes to difference between two entities with the same Id should be included in the hash.</remarks>
        Func<T, THash> GetEntityHash { get; set; }

        /// <summary>
        /// Function to create a sequence of given length of Id hashes.
        /// </summary>
        Func<TId, uint, IEnumerable<TIdHash>> IdHashes { get; set; }

        /// <summary>
        /// Perform a XOR between identifiers.
        /// </summary>
        Func<TId, TId, TId> IdXor { get; set; }

        /// <summary>
        /// <c>true</c> when the value is the identity for the identifiers (for example 0 when the identifier is a number).
        /// </summary>
        Func<TId, bool> IsIdIdentity { get; set; }

        /// <summary>
        /// Function to get the identifier for a given entity.
        /// </summary>
        Func<T, TId> GetId { get; set; }

        /// <summary>
        /// <c>true</c> when the argument is the identity value for the entity hash, else <c>false</c>.
        /// </summary>
        Func<THash, bool> IsEntityHashIdentity { get; set; }

        /// <summary>
        /// Perform a XOR between two hashes for the entity.
        /// </summary>
        Func<THash, THash, THash> EntityHashXor { get; set; }

        /// <summary>
        /// When true, each hashed ID will go to its own storage.
        /// </summary>
        bool SplitByHash { get; set; }
    }

}
