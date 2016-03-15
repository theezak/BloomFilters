namespace TBag.BloomFilters
{
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// Interface for configuration of a Bloom filter.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The identifier type</typeparam>
    /// <typeparam name="TEntityHash"></typeparam>
    /// <typeparam name="THash">The hash value type</typeparam>
    /// <typeparam name="TCount">The occurence count type.</typeparam>
    public interface IBloomFilterConfiguration<TEntity, TId, TEntityHash, THash, TCount> :
        IBloomFilterSizeConfiguration
        where TEntityHash : struct
        where THash : struct
        where TCount : struct
        where TId : struct
    {
        /// <summary>
        /// The equality comparer for <typeparamref name="TEntityHash"/>.
        /// </summary>
        EqualityComparer<TEntityHash> EntityHashEqualityComparer { get; set; }

        /// <summary>
        /// The equality comparer for <typeparamref name="THash"/>.
        /// </summary>
        EqualityComparer<THash> IdHashEqualityComparer { get; set; }
        
        /// <summary>
        /// Configuration for the Bloom filter that hashes values.
        /// </summary>
        /// <remarks>Only used by a hybrid IBF that utilizes both an IBF and a reverse IBF (this being the data for the reverse IBF)</remarks>
        IBloomFilterConfiguration<TEntity, TEntityHash, TId, THash, TCount> ValueFilterConfiguration { get;  }

        /// <summary>
        /// Function to create a sequence of given length of Id hashes.
        /// </summary>
        Func<TId, uint, IEnumerable<THash>> IdHashes { get; set; }

        /// <summary>
        /// Determine if the location in the given data is pure.
        /// </summary>
        Func<IInvertibleBloomFilterData<TId, TEntityHash, TCount>, long,  bool> IsPure { get; set; }

        /// <summary>
        /// Function to create a sequence of given length of entity hashes.
        /// </summary>
        Func<TEntity, uint, IEnumerable<TEntityHash>> EntityHashes { get; set; }

        /// <summary>
        /// Perform a XOR between identifiers.
        /// </summary>
        Func<TId, TId, TId> IdXor { get; set; }

        /// <summary>
        /// The identity value for <typeparamref name="TId"/> (for example 0 when the identifier is a number).
        /// </summary>
        Func<TId> IdIdentity { get; set; }

        /// <summary>
        /// The identity value for <typeparamref name="TEntityHash"/> (for example 0 when the identifier is a number).
        /// </summary>
        Func<TEntityHash> EntityHashIdentity { get; set; }

        /// <summary>
        /// Function to get the identifier for a given entity.
        /// </summary>
        Func<TEntity, TId> GetId { get; set; }

        /// <summary>
        /// Equality comparer for <typeparamref name="TId"/>.
        /// </summary>
        EqualityComparer<TId> IdEqualityComparer { get; set; }

        /// <summary>
        /// Perform a XOR between two hashes for the entity.
        /// </summary>
        Func<TEntityHash, TEntityHash, TEntityHash> EntityHashXor { get; set; }

        /// <summary>
        /// Function to provide the count unity
        /// </summary>
        Func<TCount> CountUnity { get; set; }

        /// <summary>
        /// Function to determine if the count is pure.
        /// </summary>
        Func<TCount,bool> IsPureCount { get; set; }

        /// <summary>
        /// Decrease the count by 1
        /// </summary>
        /// <remarks>Not the regular subtraction: this subtraction always needs to get you closer to zero, even when the value is negative.</remarks>
        Func<TCount,TCount> CountDecrease { get; set; }

        /// <summary>
        /// Get the count identity (zero for numbers)
        /// </summary>
        Func<TCount> CountIdentity { get; set; }

        /// <summary>
        /// Subtract two counts
        /// </summary>
          Func<TCount,TCount,TCount> CountSubtract { get; set; }

        /// <summary>
        /// Count equality comparer
        /// </summary>
        EqualityComparer<TCount> CountEqualityComparer { get; set; }

        /// <summary>
        /// Increase the count by 1
        /// </summary>
        /// <remarks>Not the regular add: this add should get you further away from zero, even when the value is negative.</remarks>
         Func<TCount, TCount> CountIncrease { get; set; }

        /// <summary>
        /// Determine if the configuration supports the given capacity and set size.
        /// </summary>
        /// <param name="capacity">Capacity for the Bloom filter</param>
        /// <param name="size">The actual set size.</param>
        /// <returns></returns>
        bool Supports(ulong capacity, ulong size);
    }

}
