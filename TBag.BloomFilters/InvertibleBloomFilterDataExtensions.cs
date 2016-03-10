

using System.Runtime.Serialization.Json;
using System.Timers;

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
            if (filter == null) return false;
            if (filter.Counts == null ||
                filter.HashSums == null ||
                filter.IdSums == null) return false;
            if (filter.Counts.LongLength != filter.HashSums.LongLength ||
                filter.Counts.LongLength != filter.IdSums.LongLength) return false;
            if (filter.BlockSize * filter.HashFunctionCount != filter.Counts.LongLength) return false;
            return true;
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
        /// <param name="listA">Items in <paramref name="filter"/>, but not in <paramref name="subtractedFiler"/></param>
        /// <param name="listB">Items in <paramref name="subtractedFilter"/>, but not in <paramref name="filter"/></param>
        /// <param name="modifiedEntities">items in both filters, but with a different value.</param>
        /// <param name="destructive"></param>
        /// <returns></returns>
        internal static IInvertibleBloomFilterData<TId, TCount> Subtract<TEntity,  TId, TCount>(
            this IInvertibleBloomFilterData<TId, TCount> filterData,
            IInvertibleBloomFilterData<TId, TCount> otherFilterData,
            IBloomFilterConfiguration<TEntity, int, TId, long, TCount> configuration,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities,
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
            var countsIdentity = configuration.CountIdentity();
            for (long i = 0L; i < filterData.Counts.LongLength; i++)
            {
                var filterPure = configuration.IsPureCount(filterData.Counts[i]);
                result.Counts[i] = configuration.CountSubtract(filterData.Counts[i], otherFilterData.Counts[i]);
                result.HashSums[i] = configuration.EntityHashXor(filterData.HashSums[i], otherFilterData.HashSums[i]);
                var idXorResult = configuration.IdXor(filterData.IdSums[i], otherFilterData.IdSums[i]);
                var resultPure = configuration.IsPureCount(result.Counts[i]);
                var resultZero = countEqualityComparer.Equals(result.Counts[i], countsIdentity);
                if (filterPure && 
                    !resultPure &&
                    !resultZero)
                {
                    listA.Add(filterData.IdSums[i]);
                }
                if (configuration.IsPureCount(otherFilterData.Counts[i]))
                {
                    if (resultZero)
                    {
                        //pure count went to zero
                        if (!configuration.IsIdIdentity(idXorResult))
                        {
                            listA.Add(filterData.IdSums[i]);
                            listB.Add(otherFilterData.IdSums[i]);
                            //set Id to identity, this is not a decode error.
                            idXorResult = configuration.IdXor(idXorResult, idXorResult);
                        }
                        else if (!configuration.IsEntityHashIdentity(result.HashSums[i]))
                        {
                            modifiedEntities.Add(filterData.IdSums[i]);
                        }
                        //any hash sum difference is not a decode error
                        result.HashSums[i] = 0;
                    }
                    else if (!resultPure)
                    {
                        listB.Add(otherFilterData.IdSums[i]);
                    }
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
        /// <param name="listA">Items in <paramref name="filter"/>, but not in <paramref name="subtractedFiler"/></param>
        /// <param name="listB">Items in <paramref name="subtractedFilter"/>, but not in <paramref name="filter"/></param>
        /// <param name="modifiedEntities">items in both filters, but with a different value.</param>
        /// <returns></returns>
        internal static bool Decode<TEntity, TId, TCount>(this IInvertibleBloomFilterData<TId, TCount> filter,
            IBloomFilterConfiguration<TEntity, int, TId, long, TCount> configuration,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities)
            where TCount : struct
        {
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
                //a slight bet, might be a hash from a difference.
                var hashedValue = filter.HashSums[pureIdx];
                //the difference has been accounted for, zero out.
                filter.Counts[pureIdx] = countsIdentity;
                filter.HashSums[pureIdx] = 0;
                filter.IdSums[pureIdx] = configuration.IdXor(id, id);
                foreach (var position in configuration
                    .IdHashes(id, filter.HashFunctionCount)
                    .Select(p => p%filter.Counts.LongLength)
                    .Where(p => p != pureIdx))
                {
                    if (configuration.IsPureCount(filter.Counts[position]))
                    {
                        //just before zero-ing out the count.
                        var hashEquals = filter.HashSums[position] == hashedValue;
                        var identityEquals = configuration
                            .IsIdIdentity(configuration.IdXor(id, filter.IdSums[position]));
                        if (!hashEquals || !identityEquals)
                        {
                            //pure, hash/identity is different.
                            if (!identityEquals && hashEquals)
                            {
                                if (countComparer.Compare(filter.Counts[position], countsIdentity) > 0)
                                {
                                    listA.Add(filter.IdSums[position]);
                                }
                                else
                                {
                                    listB.Add(filter.IdSums[position]);
                                }
                            }
                            else if (!listA.Contains(filter.IdSums[position]) &&
                                !listB.Contains(filter.IdSums[position]))
                            {
                                modifiedEntities.Add(filter.IdSums[position]);
                            }
                        }
                    }
                    filter.Remove(configuration, id, hashedValue, position);
                    if (configuration.IsPureCount(filter.Counts[position]) &&
                        pureList.All(p => p != position))
                    {
                        //count became pure, add to the list.
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
                .Subtract(subtractedFilter, configuration, listA, listB, modifiedEntities, destructive)
                .Decode(configuration, listA, listB, modifiedEntities);
        }

        internal static InvertibleBloomFilterData<TId, TCount> ConvertToBloomFilterData<TId, TCount>(
            this IInvertibleBloomFilterData<TId, TCount> filterData)
            where TCount : struct
        {
            if (filterData == null) return null;
            var result = filterData as InvertibleBloomFilterData<TId, TCount>;
            if (result != null) return result;
            return new InvertibleBloomFilterData<TId, TCount>
            {
                BlockSize = filterData.BlockSize,
                Counts = filterData.Counts,
                HashFunctionCount = filterData.HashFunctionCount,
                HashSums = filterData.HashSums,
                IdSums = filterData.IdSums
            };
        }
    }
}
