namespace TBag.BloomFilters
{
    using System.Collections.Generic;

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
        /// <param name="item"></param>
        void Add(T item);

        /// <summary>
        /// Determine if the item is in the Bloom filter.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        bool Contains(T item);

        /// <summary>
        /// Subtract and then decode.
        /// </summary>
        /// <param name="filter">Bloom filter to subtract</param>
        /// <param name="listA">Items in this filter, but not in <paramref name="filter"/></param>
        /// <param name="listB">Items not in this filter, but in <paramref name="filter"/></param>
        /// <param name="modifiedEntities">Entities in both filters, but with a different value</param>
        /// <returns><c>true</c> when the decode was successful, otherwise <c>false</c></returns>
        bool SubtractAndDecode(IInvertibleBloomFilterData<TId, int, TCount> filter,
            HashSet<TId> listA, 
            HashSet<TId> listB, 
            HashSet<TId> modifiedEntities);

        /// <summary>
        /// Extract the Bloom filter data in a serializable format.
        /// </summary>
        /// <returns></returns>
        InvertibleBloomFilterData<TId, int, TCount> Extract();

        /// <summary>
        /// Rehydrate the Bloom filter data.
        /// </summary>
        /// <param name="data"></param>
        void Rehydrate(IInvertibleBloomFilterData<TId, int, TCount> data);

        /// <summary>
        /// Remove the entity from the Bloom filter.
        /// </summary>
        /// <param name="item"></param>
        void Remove(T item);

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

    }
}