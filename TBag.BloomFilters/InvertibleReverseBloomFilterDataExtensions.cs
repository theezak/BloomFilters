using System.Diagnostics.Contracts;

namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extensions specific to the RIBF.
    /// </summary>
    public static class InvertibleReverseBloomFilterDataExtensions
    {
        /// <summary>
        /// Subtract, but return hash values.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TEntityHash">The type of the entity hash</typeparam>
        /// <typeparam name="THash">The type of the hash</typeparam>
        /// <typeparam name="TCount">The type of the Bloom filter occurence count</typeparam>
        /// <param name="filterData">The IBF data</param>
        /// <param name="subtractedFilterData">The IBF to subtract</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="listA">List of identifiers only in <paramref name="filterData"/></param>
        /// <param name="listB">List of identifiers only in <paramref name="subtractedFilterData"/></param>
        /// <param name="modifiedEntities">List of identifiers in both filters, but with a different value</param>
        /// <param name="pureList">Optional pure list</param>
        /// <param name="destructive">When <c>true</c> the <paramref name="filterData"/> will be used to store the subtraction results. This reduces processing overhead, but is destructive to the filter. When <c>false</c> the result will be stored in a new IBF.</param>
        /// <returns></returns>
        internal static IInvertibleBloomFilterData<TId, TEntityHash, TCount> HashSubtract<TEntity, TId, TEntityHash, THash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filterData,
            IInvertibleBloomFilterData<TId, TEntityHash, TCount> subtractedFilterData,
            IBloomFilterConfiguration<TEntity, TId, TEntityHash, THash, TCount> configuration,
               HashSet<TEntityHash> listA,
            HashSet<TEntityHash> listB,
           HashSet<TEntityHash> modifiedEntities,
           Stack<long> pureList = null,
            bool destructive = false)
            where TCount : struct
            where TId : struct
            where TEntityHash : struct
            where THash : struct
        {
            if (!filterData.IsCompatibleWith(subtractedFilterData))
                throw new ArgumentException("Subtracted invertible Bloom filters are not compatible.", nameof(subtractedFilterData));
            Contract.Assume(filterData!=null);
            var result = destructive
                ? filterData
                : new InvertibleBloomFilterData<TId, TEntityHash, TCount>
                {
                    BlockSize = filterData.BlockSize,
                    Counts = new TCount[filterData.Counts.LongLength],
                    HashFunctionCount = filterData.HashFunctionCount,
                    HashSums = new TEntityHash[filterData.HashSums.LongLength],
                    IdSums = new TId[filterData.IdSums.LongLength]
                };
            var countsIdentity = configuration.CountIdentity();
            for (var i = 0L; i < filterData.Counts.LongLength; i++)
            {
                result.Counts[i] = configuration.CountSubtract(filterData.Counts[i], subtractedFilterData.Counts[i]);
                var hashSum = configuration.EntityHashXor(filterData.HashSums[i], subtractedFilterData.HashSums[i]);
                var idXorResult = configuration.IdXor(filterData.IdSums[i], subtractedFilterData.IdSums[i]);
                if (configuration.IsPureCount(subtractedFilterData.Counts[i]) &&
                    configuration.CountEqualityComparer.Equals(result.Counts[i], countsIdentity))
                {
                    if (!configuration.EntityHashEqualityComparer.Equals(configuration.EntityHashIdentity(), hashSum))
                    {
                        listA.Add(filterData.HashSums[i]);
                        listB.Add(subtractedFilterData.HashSums[i]);
                        hashSum = configuration.EntityHashIdentity();
                        idXorResult = configuration.IdIdentity();
                    }
                    else if (!configuration.IdEqualityComparer.Equals(configuration.IdIdentity(), idXorResult))
                    {
                        modifiedEntities.Add(subtractedFilterData.HashSums[i]);
                        idXorResult = configuration.IdIdentity();
                    }

                }
                result.HashSums[i] = hashSum;
                result.IdSums[i] = idXorResult;
                if (configuration.IsPure(result, i))
                {
                    pureList?.Push(i);
                }
            }
            return result;
        }

        /// <summary>
        /// Decode the filter, but return hash values.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the occurence count for the invertible Bloom filter.</typeparam>
        /// <typeparam name="TEntityHash"></typeparam>
        /// <param name="filter">The Bloom filter data to decode</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="listA">Items in the original set, but not in the subtracted set.</param>
        /// <param name="listB">Items not in the original set, but in the subtracted set.</param>
        /// <param name="modifiedEntities">items in both sets, but with a different value.</param>
        /// <param name="pureList">Optional list of pure items.</param>
        /// <returns></returns>
        internal static bool HashDecode<TEntity, TId, TEntityHash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filter,
            IBloomFilterConfiguration<TEntity, TId, TEntityHash, int, TCount> configuration,
             HashSet<TEntityHash> listA,
            HashSet<TEntityHash> listB,
            HashSet<TEntityHash> modifiedEntities,
            Stack<long> pureList = null)
            where TEntityHash : struct
            where TId : struct
            where TCount : struct
        {
            var countComparer = Comparer<TCount>.Default;
            if (pureList == null)
            {
                pureList = new Stack<long>(Range(0L, filter.Counts.LongLength)
                 .Where(i => configuration.IsPure(filter, i))
                 .Select(i => i));
            }
            var countsIdentity = configuration.CountIdentity();
            while (pureList.Any())
            {
                var pureIdx = pureList.Pop();
                if (!configuration.IsPure(filter, pureIdx))
                {
                    continue;
                }
                var id = filter.IdSums[pureIdx];
                var hashSum = filter.HashSums[pureIdx];
                var count = filter.Counts[pureIdx];
                var negCount = countComparer.Compare(count, countsIdentity) < 0;
                var isModified = false;
                foreach (var position in configuration
                    .IdHashes(id, filter.HashFunctionCount)
                    .Select(p => Math.Abs(p % filter.Counts.LongLength))
                    .Where(p => !configuration.CountEqualityComparer.Equals(filter.Counts[p], countsIdentity)))
                {
                    if (configuration.IsPure(filter, position) &&
                        configuration
                            .EntityHashEqualityComparer.Equals(filter.HashSums[position], hashSum) &&
                        !configuration.IdEqualityComparer.Equals(filter.IdSums[position], id))
                    {
                        modifiedEntities.Add(hashSum);
                        isModified = true;
                        if (negCount)
                        {
                            filter.Add(configuration, filter.IdSums[position], hashSum, position);
                        }
                        else
                        {
                            filter.Remove(configuration, filter.IdSums[position], hashSum, position);
                        }
                    }
                    else
                    {
                        if (negCount)
                        {
                            filter.Add(configuration, id, hashSum, position);
                        }
                        else
                        {
                            filter.Remove(configuration, id, hashSum, position);
                        }
                    }
                    if (configuration.IsPure(filter, position))
                    {
                        //count became pure, add to the list.
                        pureList.Push(position);
                    }
                }
                if (!isModified)
                {
                    if (negCount)
                    {
                        listB.Add(hashSum);
                    }
                    else
                    {
                        listA.Add(hashSum);
                    }
                }
            }
            modifiedEntities.MoveModified(listA, listB);
            return filter.IsCompleteDecode(configuration);
        }

        /// <summary>
        /// Subtract the given filter and decode for any changes
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the Bloom filter occurence count</typeparam>
        /// <typeparam name="TEntityHash"></typeparam>
        /// <param name="filter">Filter</param>
        /// <param name="subtractedFilter">The Bloom filter to subtract</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="listA">Items in <paramref name="filter"/>, but not in <paramref name="subtractedFilter"/></param>
        /// <param name="listB">Items in <paramref name="subtractedFilter"/>, but not in <paramref name="filter"/></param>
        /// <param name="modifiedEntities">items in both filters, but with a different value.</param>
        /// <param name="destructive">Optional parameter, when <c>true</c> the filter <paramref name="filter"/> will be modified, and thus rendered useless, by the decoding.</param>
        /// <returns></returns>
        public static bool HashSubtractAndDecode<TEntity, TId, TEntityHash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filter,
            IInvertibleBloomFilterData<TId, TEntityHash, TCount> subtractedFilter,
            IBloomFilterConfiguration<TEntity, TId, TEntityHash, int, TCount> configuration,
            HashSet<TEntityHash> listA,
            HashSet<TEntityHash> listB,
           HashSet<TEntityHash> modifiedEntities,
            bool destructive = false)
            where TId : struct
            where TCount : struct
            where TEntityHash : struct
        {
            if (filter == null || subtractedFilter == null) return true;
            var pureList = new Stack<long>();
            return filter
               .HashSubtract(subtractedFilter, configuration, listA, listB, modifiedEntities, pureList, destructive)
                .HashDecode(configuration, listA, listB, modifiedEntities, pureList);
        }

        private static IEnumerable<long> Range(long start, long end)
        {
            for (long i = start; i < end; i++)
                yield return i;
        }
    }
}