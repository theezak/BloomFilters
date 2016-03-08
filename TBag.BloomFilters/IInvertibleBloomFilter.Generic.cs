namespace TBag.BloomFilters
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface for an invertible Bloom filter.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    public interface IInvertibleBloomFilter<T, TId, TCount>
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
        /// Decode the Bloom filter.
        /// </summary>
        /// <param name="listA">identifiers of entities in this filter, but not in the subtracted filter.</param>
        /// <param name="listB">identifiers of ntities in the subtracted filter, but not in this filter.</param>
        /// <param name="modifiedEntities">identifiers of entities in both filters, but with a different value.</param>
        /// <returns></returns>
        /// <remarks>Currently destructive.</remarks>
        bool Decode(HashSet<TId> listA, HashSet<TId> listB, HashSet<TId> modifiedEntities);

        /// <summary>
        /// Extract the Bloom filter data in a serializable format.
        /// </summary>
        /// <returns></returns>
        IInvertibleBloomFilterData<TId, TCount> Extract();

        /// <summary>
        /// Rehydrate the Bloom filter data.
        /// </summary>
        /// <param name="data"></param>
        void Rehydrate(IInvertibleBloomFilterData<TId, TCount> data);

        /// <summary>
        /// Remove the entity from the Bloom filter.
        /// </summary>
        /// <param name="item"></param>
        void Remove(T item);

        /// <summary>
        /// Subtract two Bloom filters 
        /// </summary>
        /// <param name="filter">The Bloom filter to subtract.</param>
        /// <param name="idsWithChanges">identifiers of entities recognized to be in both sets of identifiers, but with a different value.</param>
        /// <remarks>Result is the difference between the two Bloom filters</remarks>
        void Subtract(InvertibleBloomFilter<T, TId, TCount> filter, HashSet<TId> idsWithChanges = null);

        /// <summary>
        /// Subtract the Bloom filter data
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="idsWithChanges"></param>
        /// <remarks>Result is the difference between the two Bloom filters.</remarks>
        void Subtract(IInvertibleBloomFilterData<TId, TCount> filter, HashSet<TId> idsWithChanges = null);
    }
}