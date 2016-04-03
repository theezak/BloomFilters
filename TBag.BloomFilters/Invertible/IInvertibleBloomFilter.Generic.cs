namespace TBag.BloomFilters.Invertible
{
    using System.Collections.Generic;
    using System;
    using System.Threading;

    /// <summary>
    /// Interface for an invertible Bloom filter.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <typeparam name="TCount"></typeparam>
    public interface IInvertibleBloomFilter<T, TId, TCount>
        where TId : struct
        where TCount : struct
    {
        /// <summary>
        /// Add an entity to the Bloom filter.
        /// </summary>
        /// <param name="item">The entity to add</param>
        /// <exception cref="InvalidOperationException">When the Bloom filter configuration is not valid.</exception>
        void Add(T item);

        /// <summary>
        /// Add the Bloom filter.
        /// </summary>
        /// <param name="bloomFilter"></param>
        /// <exception cref="ArgumentException">Bloom filter is not compatible</exception>
        void Add(IInvertibleBloomFilter<T, TId, TCount> bloomFilter);

        /// <summary>
        /// Add the Bloom filter data
        /// </summary>
        /// <param name="bloomFilterData"></param>
        /// <exception cref="ArgumentException">Bloom filter data is not compatible</exception>
        void Add(IInvertibleBloomFilterData<TId, int, TCount> bloomFilterData);

        /// <summary>
        /// Fold the Bloom filter
        /// </summary>
        /// <param name="factor">The factor to fold the Bloom filter by</param>
        /// <param name="destructive">When <c>true</c> the Bloom filter is replaced by the folded Bloom filter, else <c>false</c>.</param>
        /// <exception cref="ArgumentException">The Bloom filter cannot be folded by the given factor.</exception>
        IInvertibleBloomFilter<T, TId, TCount> Fold(uint factor, bool destructive = false);

        /// <summary>
        /// Determine if the item is in the Bloom filter.
        /// </summary>
        /// <param name="item">The entity</param>
        /// <returns><c>true</c> when the Bloom filter contains the item, else <c>false</c></returns>
        /// <remarks>False-positives are possible</remarks>
        /// <exception cref="InvalidOperationException">When the Bloom filter configuration is not valid.</exception>
        bool Contains(T item);

        /// <summary>
        /// Determine if the given key is in the Bloom filter.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns><c>true</c> when the Bloom filter contains the given key, else <c>false</c></returns>
        /// <exception cref="NotSupportedException">When the Bloom filter does no support look-up by key</exception>
        /// <exception cref="InvalidOperationException">When the Bloom filter configuration is not valid.</exception>
        /// <remarks>False-positives are possible</remarks>
        bool ContainsKey(TId key);

        /// <summary>
        /// Subtract and then decode.
        /// </summary>
        /// <param name="filterData">Bloom filter data to subtract</param>
        /// <param name="listA">Items in this filter, but not in <paramref name="filterData"/></param>
        /// <param name="listB">Items not in this filter, but in <paramref name="filterData"/></param>
        /// <param name="modifiedEntities">Entities in both filters, but with a different value</param>
        /// <returns><c>true</c> when the decode was successful, otherwise <c>false</c></returns>
        bool SubtractAndDecode(
            HashSet<TId> listA, 
            HashSet<TId> listB, 
            HashSet<TId> modifiedEntities,
            IInvertibleBloomFilterData<TId, int, TCount> filterData);

        /// <summary>
        /// Extract the Bloom filter data in a serializable format.
        /// </summary>
        /// <returns>The Bloom filter data</returns>
        InvertibleBloomFilterData<TId, int, TCount> Extract();

        /// <summary>
        /// Rehydrate the Bloom filter data.
        /// </summary>
        /// <param name="data">The data to restore</param>
        /// <exception cref="ArgumentException">When the data is not valid.</exception>       
        void Rehydrate(IInvertibleBloomFilterData<TId, int, TCount> data);

        /// <summary>
        /// Remove the entity from the Bloom filter.
        /// </summary>
        /// <param name="item">The entity to remove</param>
        /// <exception cref="InvalidOperationException">When the Bloom filter configuration is not valid.</exception>
        void Remove(T item);

        /// <summary>
        /// Remove by key
        /// </summary>
        /// <param name="key">The key to remove</param>
        /// <exception cref="NotSupportedException">When the Bloom filter does no support removal by key</exception>
        /// <exception cref="InvalidOperationException">When the Bloom filter configuration is not valid.</exception>
        void RemoveKey(TId key);

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="capacity">The capacity (number of elements stored in the Bloom filter)</param>
        /// <param name="foldFactor"></param>
        void Initialize(long capacity, int foldFactor = 0);

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="capacity">The capacity (number of elements stored in the Bloom filter)</param>
        /// <param name="errorRate">The error rate (between 0 and 1)</param>
        /// <param name="foldFactor"></param>
        void Initialize(long capacity, float errorRate, int foldFactor = 0);

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="capacity">The capacity (number of elements stored in the Bloom filter)</param>
        /// <param name="m">Size per hash function</param>
        /// <param name="k">Hash function count</param>
        void Initialize(long capacity, long m, uint k);

        /// <summary>
        /// Subtract and then decode.
        /// </summary>
        /// <param name="filter">Bloom filter to subtract</param>
        /// <param name="listA">Items in this filter, but not in <paramref name="filter"/></param>
        /// <param name="listB">Items not in this filter, but in <paramref name="filter"/></param>
        /// <param name="modifiedEntities">Entities in both filters, but with a different value</param>
        /// <returns><c>true</c> when the decode was successful, otherwise <c>false</c></returns>
        bool SubtractAndDecode(IInvertibleBloomFilter<T, TId, TCount> filter, HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities);

        /// <summary>
        /// Compress the Bloom filter
        /// </summary>
        IInvertibleBloomFilter<T, TId, TCount> Compress(bool inPlace = false);

        /// <summary>
        /// The number of items in the Bloom filter.
        /// </summary>
        long ItemCount { get; }

    }
}