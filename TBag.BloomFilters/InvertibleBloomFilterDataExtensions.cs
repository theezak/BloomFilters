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
        /// <typeparam name="TId">The type of entity identifier</typeparam>
        /// <typeparam name="TEntityHash">The type of the entity hash.</typeparam>
        /// <typeparam name="TCount">The type of the occurence counter for the invertible Bloom filter.</typeparam>
        /// <param name="filter"></param>
        /// <param name="otherFilter"></param>
        /// <returns></returns>
        public static bool IsCompatibleWith<TId, TEntityHash, TCount>(this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filter,
            IInvertibleBloomFilterData<TId, TEntityHash, TCount> otherFilter)
            where TId : struct
            where TEntityHash : struct
            where TCount : struct
        {
            if (!filter.IsValid() || !otherFilter.IsValid()) return false;
            return filter.BlockSize == otherFilter.BlockSize &&
                filter.IsReverse == otherFilter.IsReverse &&
               filter.HashFunctionCount == otherFilter.HashFunctionCount &&
               filter.Counts.LongLength == otherFilter.Counts.LongLength &&
               filter.HashSums?.LongLength == otherFilter.HashSums?.LongLength &&
               (filter.ValueFilter == otherFilter.ValueFilter ||
               filter.ValueFilter.IsCompatibleWith(otherFilter.ValueFilter));
        }

        /// <summary>
        /// <c>true</c> when the filter is valid, else <c>false</c>.
        /// </summary>
        /// <typeparam name="TId">The type of entity identifier</typeparam>
        /// <typeparam name="TEntityHash">The type of the entity hash.</typeparam>
        /// <typeparam name="TCount">The type of the occurence counter for the invertible Bloom filter.</typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static bool IsValid<TId, TEntityHash, TCount>(this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filter)
            where TCount : struct
            where TEntityHash : struct
            where TId : struct
        {
            if (filter?.Counts == null ||
                filter.IdSums == null ||
                filter.HashSums == null) return false;
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
        /// <typeparam name="TEntityHash"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <param name="filterData"></param>
        /// <param name="subtractedFilterData"></param>
        /// <param name="configuration"></param>
        /// <param name="listA">Items in <paramref name="filterData"/>, but not in <paramref name="subtractedFilterData"/></param>
        /// <param name="listB">Items in <paramref name="subtractedFilterData"/>, but not in <paramref name="filterData"/></param>
        /// <param name="modifiedEntities">items in both filters, but with a different value.</param>
        /// <param name="destructive"></param>
        /// <returns></returns>
        internal static IInvertibleBloomFilterData<TId, TEntityHash, TCount> Subtract<TEntity, TId, TEntityHash, THash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filterData,
            IInvertibleBloomFilterData<TId, TEntityHash, TCount> subtractedFilterData,
            IBloomFilterConfiguration<TEntity, TId, TEntityHash, THash, TCount> configuration,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities,
            bool destructive = false
            )
            where TCount : struct
            where TId : struct
            where TEntityHash : struct
            where THash : struct
        {
            var result = destructive ? filterData : new InvertibleBloomFilterData<TId, TEntityHash, TCount>
            {
                BlockSize = filterData.BlockSize,
                Counts = new TCount[filterData.Counts.LongLength],
                HashFunctionCount = filterData.HashFunctionCount,
                HashSums = filterData.HashSums == null ? null : new TEntityHash[filterData.HashSums.LongLength],
                IdSums = new TId[filterData.IdSums.LongLength]
            };
            if (!filterData.IsCompatibleWith(subtractedFilterData))
                throw new ArgumentException("Subtracted invertible Bloom filters are not compatible.", nameof(subtractedFilterData));
            var countsIdentity = configuration.CountIdentity();
            for (long i = 0L; i < filterData.Counts.LongLength; i++)
            {
                result.Counts[i] = configuration.CountSubtract(filterData.Counts[i], subtractedFilterData.Counts[i]);
                var hashSum = configuration.EntityHashXor(
                    filterData.HashSums[i],
                    subtractedFilterData.HashSums[i]);
                var idXorResult = configuration.IdXor(filterData.IdSums[i], subtractedFilterData.IdSums[i]);                
                result.HashSums[i] = configuration.EntityHashXor(filterData.HashSums[i],
                        subtractedFilterData.HashSums[i]);
                if (configuration.IsPureCount(subtractedFilterData.Counts[i]) &&
                    configuration.CountEqualityComparer.Equals(result.Counts[i], countsIdentity))
                {
                    //pure count went to zero: both filters were pure at the given position.
                    if (!configuration.IsIdIdentity(idXorResult))
                    {
                        listA.Add(filterData.IdSums[i]);
                        listB.Add(subtractedFilterData.IdSums[i]);
                        idXorResult = configuration.IdXor(idXorResult, idXorResult);
                        hashSum = configuration.EntityHashXor(hashSum, hashSum);
                    }
                    else if (!configuration.IsEntityHashIdentity(hashSum))
                    {
                        modifiedEntities.Add(subtractedFilterData.IdSums[i]);
                        hashSum = configuration.EntityHashXor(hashSum, hashSum);
                    }                                        
                }
                result.HashSums[i] = hashSum;
                result.IdSums[i] = idXorResult;
            }
            return result;
        }

        /// <summary>
        /// Subtract, but return hash values.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TEntityHash"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="filterData"></param>
        /// <param name="subtractedFilterData"></param>
        /// <param name="configuration"></param>
        /// <param name="listA"></param>
        /// <param name="listB"></param>
        /// <param name="modifiedEntities"></param>
        /// <param name="destructive"></param>
        /// <returns></returns>
        internal static IInvertibleBloomFilterData<TId, TEntityHash, TCount> HashSubtract<TEntity, TId, TEntityHash, THash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filterData,
            IInvertibleBloomFilterData<TId, TEntityHash, TCount> subtractedFilterData,
            IBloomFilterConfiguration<TEntity, TId, TEntityHash, THash, TCount> configuration,
               HashSet<TEntityHash> listA,
            HashSet<TEntityHash> listB,
           HashSet<TEntityHash> modifiedEntities,
            bool destructive = false)
            where TCount : struct
            where TId : struct
            where TEntityHash : struct
            where THash : struct
        {
            var result = destructive
                ? filterData
                : new InvertibleBloomFilterData<TId, TEntityHash, TCount>
                {
                    BlockSize = filterData.BlockSize,
                    Counts = new TCount[filterData.Counts.LongLength],
                    HashFunctionCount = filterData.HashFunctionCount,
                    HashSums = filterData.HashSums == null ? null : new TEntityHash[filterData.HashSums.LongLength],
                    IdSums = new TId[filterData.IdSums.LongLength]
                };
            if (!filterData.IsCompatibleWith(subtractedFilterData))
                throw new ArgumentException("Subtracted invertible Bloom filters are not compatible.", nameof(subtractedFilterData));
            var countsIdentity = configuration.CountIdentity();
            for (long i = 0L; i < filterData.Counts.LongLength; i++)
            {
                result.Counts[i] = configuration.CountSubtract(filterData.Counts[i], subtractedFilterData.Counts[i]);
                var hashSum = configuration.EntityHashXor(filterData.HashSums[i], subtractedFilterData.HashSums[i]);
                var idXorResult = configuration.IdXor(filterData.IdSums[i], subtractedFilterData.IdSums[i]);
                if (configuration.IsPureCount(subtractedFilterData.Counts[i]) &&
                    configuration.CountEqualityComparer.Equals(result.Counts[i], countsIdentity))
                {
                    if (!configuration.IsEntityHashIdentity(hashSum))
                    {
                        listA.Add(filterData.HashSums[i]);
                        listB.Add(subtractedFilterData.HashSums[i]);
                        hashSum = configuration.EntityHashXor(hashSum, hashSum);
                        idXorResult = configuration.IdXor(idXorResult, idXorResult);
                    }
                    else if (!configuration.IsIdIdentity(idXorResult))
                    {
                        modifiedEntities.Add(subtractedFilterData.HashSums[i]);
                        idXorResult = configuration.IdXor(idXorResult, idXorResult);
                    }
                    
                }
                result.HashSums[i] = hashSum;
               result.IdSums[i] = idXorResult;
            }
            return result;
        }

        /// <summary>
        /// Decode the filter.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the occurence count for the invertible Bloom filter.</typeparam>
        /// <param name="filter">The Bloom filter data to decode</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="listA">Items in the original set, but not in the subtracted set.</param>
        /// <param name="listB">Items not in the original set, but in the subtracted set.</param>
        /// <param name="modifiedEntities">items in both sets, but with a different value.</param>
        /// <returns></returns>
        internal static bool Decode<TEntity, TId, TEntityHash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filter,
            IBloomFilterConfiguration<TEntity, TId, TEntityHash, int, TCount> configuration,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities)
            where TEntityHash : struct
            where TId : struct
            where TCount : struct
        {
            var countComparer = Comparer<TCount>.Default;
            var pureList = new Stack<long>(Range(0L, filter.Counts.LongLength)
                .Where(i => configuration.IsPure(filter, i))
                .Select(i => i));
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
                        !configuration.EntityHashEqualityComparer.Equals(filter.HashSums[position], hashSum) &&
                        configuration.IdEqualityComparer.Equals(id, filter.IdSums[position]))
                    {
                        modifiedEntities.Add(id);
                        isModified = true;
                        if (negCount)
                        {
                            filter.Add(configuration, id, filter.HashSums[position], position);
                        }
                        else
                        {
                            filter.Remove(configuration, id, filter.HashSums[position], position);
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
                        listB.Add(id);
                    }
                    else
                    {
                        listA.Add(id);
                    }
                }
            }
            ModIntersection(listA, listB, modifiedEntities);
            return filter.IsCompleteDecode(configuration, countsIdentity);
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
        /// <returns></returns>
        internal static bool HashDecode<TEntity, TId, TEntityHash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filter,
            IBloomFilterConfiguration<TEntity, TId, TEntityHash, int, TCount> configuration,
             HashSet<TEntityHash> listA,
            HashSet<TEntityHash> listB,
            HashSet<TEntityHash> modifiedEntities)
            where TEntityHash : struct
            where TId : struct
            where TCount : struct
        {
           var countComparer = Comparer<TCount>.Default;
            var pureList = new Stack<long>(Range(0L, filter.Counts.LongLength)
                .Where(i => configuration.IsPure(filter, i))
                .Select(i => i));
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
                        //so... yeah. Technically, when negative, it was already subtracted and you should not subtract it again.
                        //this is assymetric. B2 has already been subtracted, B1 hasn't.
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
            ModIntersection(listA, listB, modifiedEntities);
            return filter.IsCompleteDecode(configuration, countsIdentity);
        }

        private static bool IsCompleteDecode<TEntity, TId, TEntityHash, THash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filter,
            IBloomFilterConfiguration<TEntity, TId, TEntityHash, THash, TCount> configuration,
           TCount countsIdentity)
            where TCount : struct
            where TId : struct
            where THash : struct
            where TEntityHash : struct
        {
            for (var position = 0L; position < filter.Counts.LongLength; position++)
            {
                if (!configuration.IsIdIdentity(filter.IdSums[position]) ||
                    (filter.HashSums == null || !configuration.IsEntityHashIdentity(filter.HashSums[position])) ||
                    !configuration.CountEqualityComparer.Equals(filter.Counts[position], countsIdentity))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Remove an item from the given position.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <typeparam name="TEntityHash"></typeparam>
        /// <param name="filter"></param>
        /// <param name="configuration"></param>
        /// <param name="idValue"></param>
        /// <param name="hashValue"></param>
        /// <param name="position"></param>
        internal static void Remove<TEntity, TId, TEntityHash, THash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filter,
            IBloomFilterConfiguration<TEntity, TId, TEntityHash, THash, TCount> configuration,
            TId idValue,
            TEntityHash hashValue,
            long position)
            where TCount : struct
            where TId : struct
            where THash : struct
            where TEntityHash : struct

        {
            if (filter == null) return;
            filter.Counts[position] = configuration.CountDecrease(filter.Counts[position]);
            filter.IdSums[position] = configuration.IdXor(filter.IdSums[position], idValue);
            filter.HashSums[position] = configuration.EntityHashXor(filter.HashSums[position], hashValue);
        }

        /// <summary>
        /// Add an item from the given position.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <typeparam name="TEntityHash"></typeparam>
        /// <param name="filter"></param>
        /// <param name="configuration"></param>
        /// <param name="idValue"></param>
        /// <param name="hashValue"></param>
        /// <param name="position"></param>
        internal static void Add<TEntity, TId, TEntityHash, THash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filter,
            IBloomFilterConfiguration<TEntity, TId, TEntityHash, THash, TCount> configuration,
            TId idValue,
            TEntityHash hashValue,
            long position)
            where TCount : struct
            where TId : struct
            where THash : struct
            where TEntityHash : struct

        {
            if (filter == null) return;
            filter.Counts[position] = configuration.CountIncrease(filter.Counts[position]);
            filter.IdSums[position] = configuration.IdXor(filter.IdSums[position], idValue);
            filter.HashSums[position] = configuration.EntityHashXor(filter.HashSums[position], hashValue);
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
        public static bool SubtractAndDecode<TEntity, TId, TEntityHash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filter,
            IInvertibleBloomFilterData<TId, TEntityHash, TCount> subtractedFilter,
            IBloomFilterConfiguration<TEntity, TId, TEntityHash, int, TCount> configuration,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities,
            bool destructive = false)
            where TId : struct
            where TCount : struct
            where TEntityHash : struct
        {
            if (!filter.IsCompatibleWith(subtractedFilter)) throw new ArgumentException("The subtracted Bloom filter data is not compatible with the Bloom filter.", nameof(subtractedFilter));
            var valueRes = true;
            var idRes = true;
            if (!filter.IsReverse)
            {
                idRes = filter
                    .Subtract(subtractedFilter, configuration, listA, listB, modifiedEntities, destructive)
                    .Decode(configuration, listA, listB, modifiedEntities);
            }
            var reverseFilter = filter.IsReverse ? filter.Reverse() : filter.ValueFilter;
            var reverseSubtractedFilter = subtractedFilter.IsReverse ? subtractedFilter.Reverse() : subtractedFilter.ValueFilter;
            if (reverseFilter != null &&
                reverseSubtractedFilter != null)
            {
                 valueRes = reverseFilter
                    .HashSubtractAndDecode(
                    reverseSubtractedFilter,
                    configuration.ValueFilterConfiguration,
                   listA,
                  listB,
                    modifiedEntities,
                    destructive);
            }
            return idRes && valueRes;
        }

        /// <summary>
        /// Reverse the filter data.
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TEntityHash"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static InvertibleBloomFilterData<TEntityHash, TId, TCount> Reverse<TId, TEntityHash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> data)
            where TId : struct
            where TCount : struct
            where TEntityHash : struct
        {
            if (data == null) return null;
            return new InvertibleBloomFilterData<TEntityHash, TId, TCount>
            {
                IsReverse = true,
                BlockSize = data.BlockSize,
                Counts = data.Counts,
                HashFunctionCount = data.HashFunctionCount,
                IdSums = data.HashSums,
                HashSums = data.IdSums
            };
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
            return filter
               .HashSubtract(subtractedFilter, configuration, listA, listB, modifiedEntities, destructive)
                .HashDecode(configuration, listA, listB, modifiedEntities);
        }

        /// <summary>
        /// Convert a <see cref="IInvertibleBloomFilterData{TId, TEntityHash, TCount}"/> to a concrete <see cref="InvertibleBloomFilterData{TId, TEntityHash, TCount}"/>.
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TEntityHash"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="filterData"></param>
        /// <returns></returns>
        internal static InvertibleBloomFilterData<TId, TEntityHash, TCount> ConvertToBloomFilterData<TId, TEntityHash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filterData)
            where TId : struct
            where TEntityHash : struct
            where TCount : struct
        {
            if (filterData == null) return null;
            var result = filterData as InvertibleBloomFilterData<TId, TEntityHash, TCount>;
            if (result != null) return result;
            return new InvertibleBloomFilterData<TId, TEntityHash, TCount>
            {
                BlockSize = filterData.BlockSize,
                Counts = filterData.Counts,
                HashFunctionCount = filterData.HashFunctionCount,
                HashSums = filterData.HashSums,
                IdSums = filterData.IdSums,
                ValueFilter = filterData.ValueFilter?.ConvertToBloomFilterData()
            };
        }

        private static void ModIntersection<T>(HashSet<T> listA, HashSet<T> listB, HashSet<T> modifiedEntities)
        {
            if (listA == modifiedEntities || listB == modifiedEntities) return;
            foreach (var modItem in listA.Where(itm => listB.Contains(itm)).ToArray())
            {
                listA.Remove(modItem);
                listB.Remove(modItem);
                modifiedEntities.Add(modItem);
            }
        }
    }
}
