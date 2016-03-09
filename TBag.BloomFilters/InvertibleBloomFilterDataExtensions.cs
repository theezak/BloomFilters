

namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    /// <summary>
    /// Extension methods for invertible Bloom filter data.
    /// </summary>
    public static class InvertibleBloomFilterDataExtensions
    {
        /// <summary>
        /// <c>true</c> when the filters are compatible, else <c>false</c>
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <param name="filter"></param>
        /// <param name="otherFilter"></param>
        /// <returns></returns>
        public static bool IsCompatibleWith<TId,TCount>(this IInvertibleBloomFilterData<TId, TCount> filter, 
            IInvertibleBloomFilterData<TId,TCount> otherFilter)
            where TCount : struct
        {
            if (!filter.IsValid() || !otherFilter.IsValid()) return false;
            return filter.BlockSize == otherFilter.BlockSize &&
                filter.HashFunctionCount == otherFilter.HashFunctionCount &&
                filter.Counts.LongLength == otherFilter.Counts.LongLength;
        }

        /// <summary>
        /// <c>true</c> when the filter is valid, else <c>false</c>.
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static bool IsValid<TId,TCount>(this IInvertibleBloomFilterData<TId,TCount> filter)
            where TCount : struct
        {
            if (filter == null) return true;
            if (filter.Counts == null ||
                filter.HashSums == null ||
                filter.IdSums == null) return false;
            if (filter.Counts.LongLength != filter.HashSums.LongLength ||
                filter.Counts.LongLength != filter.IdSums.LongLength) return false;
            if (filter.BlockSize * filter.HashFunctionCount != filter.Counts.LongLength &&
                filter.BlockSize != filter.Counts.LongLength) return false;
            return true;
        }

        /// <summary>
        /// <c>true</c> when the filter data was split in a row by hash function, else <c>false</c>.
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static bool HasRows<TId,TCount>(this IInvertibleBloomFilterData<TId,TCount> filter)
            where TCount :struct
        {
            if (filter == null || filter.Counts == null) return false;
            return filter.BlockSize != filter.Counts.LongLength;
        }

        private static IEnumerable<long> Range(long start, long end)
        {
            for (long i = start; i < end; i++)
                yield return i;
        }

        /// <summary>
        /// Subtract the Bloom filter data.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="filterData"></param>
        /// <param name="otherFilterData"></param>
        /// <param name="configuration"></param>
        /// <param name="idsWithChanges"></param>
        /// <param name="destructive"></param>
        /// <returns></returns>
        internal static IInvertibleBloomFilterData<TId, TCount> Subtract<TEntity,  TId, TCount>(
            this IInvertibleBloomFilterData<TId, TCount> filterData,
            IInvertibleBloomFilterData<TId, TCount> otherFilterData,
            IBloomFilterConfiguration<TEntity, int, TId, long, TCount> configuration,
            HashSet<TId> idsWithChanges = null,
            bool destructive = false
            )
            where TCount : struct
        {
            var result = destructive ? filterData : new InvertibleBloomFilterData<TId, TCount>
            {
                BlockSize = filterData.BlockSize,
                Counts = new TCount[filterData.Counts.LongLength],
                HashFunctionCount = filterData.HashFunctionCount,
                HashSums = new int[filterData.HashSums.LongLength],
                IdSums = new TId[filterData.IdSums.LongLength]
            };
            if (!filterData.IsCompatibleWith(otherFilterData))
                throw new ArgumentException("Subtracted invertible Bloom filters are not compatible.", nameof(otherFilterData));
            var countEqualityComparer = EqualityComparer<TCount>.Default;
            var detectChanges = idsWithChanges != null;
            var countsIdentity = configuration.CountIdentity();
            for (long i = 0L; i < filterData.Counts.LongLength; i++)
            {
                result.Counts[i] = configuration.CountSubtract(filterData.Counts[i], otherFilterData.Counts[i]);
                result.HashSums[i] = configuration.EntityHashXor(filterData.HashSums[i], otherFilterData.HashSums[i]);
                var idXorResult = configuration.IdXor(filterData.IdSums[i], otherFilterData.IdSums[i]);
                if (detectChanges &&
                    !configuration.IsEntityHashIdentity(filterData.HashSums[i]) &&
                    countEqualityComparer.Equals(filterData.Counts[i], countsIdentity) &&
                    configuration.IsPureCount(otherFilterData.Counts[i])  &&
                    configuration.IsIdIdentity(idXorResult))
                {
                    idsWithChanges.Add(filterData.IdSums[i]);
                    //recognized the difference, not a decode error: funky way of setting the id sum to the identity value for the identifier type.
                    result.IdSums[i] = configuration.IdXor(filterData.IdSums[i], filterData.IdSums[i]);
                    continue;
                }
                result.IdSums[i] = idXorResult;
            }
            return result;
        }

        /// <summary>
        /// Decode the 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="filter"></param>
        /// <param name="configuration"></param>
        /// <param name="listA"></param>
        /// <param name="listB"></param>
        /// <param name="modifiedEntities"></param>
        /// <returns></returns>
        internal static bool Decode<TEntity,TId,TCount>(this IInvertibleBloomFilterData<TId,TCount> filter,
            IBloomFilterConfiguration<TEntity, int, TId, long, TCount> configuration,
            HashSet<TId> listA, 
            HashSet<TId> listB, 
            HashSet<TId> modifiedEntities)
            where TCount : struct
        {
            var idMap = new Dictionary<string, HashSet<TId>>();
            var countEqualityComparer = EqualityComparer<TCount>.Default;
            var countComparer = Comparer<TCount>.Default;
            var pureList = Range(0L, filter.Counts.LongLength)
                .Where(i => configuration.IsPureCount(filter.Counts[i]))
                .Select(i => i)
                .ToList();
            var countsIdentity = configuration.CountIdentity();
            while (pureList.Any())
            {
                var pureIdx = pureList[0];
                pureList.RemoveAt(0);
                if (!configuration.IsPureCount(filter.Counts[pureIdx]))
                {
                    if (countEqualityComparer.Equals(filter.Counts[pureIdx], countsIdentity) &&
                       !configuration.IsEntityHashIdentity(filter.HashSums[pureIdx]) &&
                       configuration.IsIdIdentity(filter.IdSums[pureIdx]) &&
                       idMap.ContainsKey($"{pureIdx}"))
                    {
                        //ID and counts nicely zeroed out, but the hash didn't. A changed value might have been hashed in.
                        //this does constitute a decode error, since we couldn't exactly identify the identity that caused the difference.
                        foreach (var associatedId in idMap[$"{pureIdx}"])
                        {
                            modifiedEntities.Add(associatedId);
                        }
                        idMap.Clear();
                    }
                    continue;
                }
                var count = filter.Counts[pureIdx];
                var id = filter.IdSums[pureIdx];
                if (countComparer.Compare(count, countsIdentity) > 0)
                {
                    listA.Add(id);
                }
                else
                {
                    listB.Add(id);
                }
                var hash3 = filter.HashSums[pureIdx];
                var idx = 0L;
                var hasRows = filter.HasRows();
                foreach (var position in configuration.IdHashes(id, filter.HashFunctionCount).Select(p =>
                {
                    var res = (p % filter.BlockSize) + idx;
                    if (hasRows)
                    {
                        idx += filter.BlockSize;
                    }
                    return res;
                }))
                {
                    filter.Remove(configuration, id, hash3, position);
                    if (!idMap.ContainsKey($"{position}"))
                    {
                        idMap[$"{position}"] = new HashSet<TId>();
                    }

                    idMap[$"{position}"].Add(id);
                    if (configuration.IsPureCount(filter.Counts[position]) && 
                        pureList.All(p => p != position))
                    {
                        pureList.Add(position);
                    }
                }
            }
            for (var position = 0L; position < filter.Counts.LongLength; position++)
            {
                if (!configuration.IsIdIdentity(filter.IdSums[position]) ||
                    !configuration.IsEntityHashIdentity(filter.HashSums[position]) ||
                        !countEqualityComparer.Equals(filter.Counts[position], countsIdentity))
                    return false;
            }
            return true;
        }

      /// <summary>
      /// Remove an item from the given position.
      /// </summary>
      /// <typeparam name="TEntity"></typeparam>
      /// <typeparam name="TId"></typeparam>
      /// <typeparam name="TCount"></typeparam>
      /// <param name="filter"></param>
      /// <param name="configuration"></param>
      /// <param name="idValue"></param>
      /// <param name="hashValue"></param>
      /// <param name="position"></param>
        internal static void Remove<TEntity,TId,TCount>(
            this IInvertibleBloomFilterData<TId,TCount> filter,
            IBloomFilterConfiguration<TEntity, int, TId, long, TCount> configuration,
            TId idValue, 
            int hashValue, 
            long position)
            where TCount : struct
        {
            filter.Counts[position] = configuration.CountDecrease(filter.Counts[position]);
            filter.IdSums[position] = configuration.IdXor(filter.IdSums[position], idValue);
           filter.HashSums[position] = configuration.EntityHashXor(filter.HashSums[position], hashValue);
        }

        /// <summary>
        /// Subtract the given filter and decode for any chanes
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the Bloom filter occurence count</typeparam>
        /// <param name="filter">Filter</param>
        /// <param name="subtractedFilter">The Bloom filter to subtract</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="listA">Items in <paramref name="filter"/>, but not in <paramref name="subtractedFiler"/></param>
        /// <param name="listB">Items in <paramref name="subtractedFilter"/>, but not in <paramref name="filter"/></param>
        /// <param name="modifiedEntities">items in both filters, but with a different value.</param>
        /// <param name="destructive">Optional parameter, when <c>true</c> the filter <paramref name="filter"/> will be modified, and thus rendered useless, by the decoding.</param>
        /// <returns></returns>
        public static bool SubtractAndDecode<TEntity, TId, TCount>(
            this IInvertibleBloomFilterData<TId, TCount> filter,
            IInvertibleBloomFilterData<TId, TCount> subtractedFilter,
            IBloomFilterConfiguration<TEntity, int, TId, long, TCount> configuration,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities,
            bool destructive = false)
            where TCount : struct
        {
            return filter
                .Subtract(subtractedFilter, configuration, modifiedEntities, destructive)
                .Decode(configuration, listA, listB, modifiedEntities);
        }
    }
}
