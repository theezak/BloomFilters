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
            var checkHash = result.HashSums != null && subtractedFilterData.HashSums != null;
            if (!filterData.IsCompatibleWith(subtractedFilterData))
                throw new ArgumentException("Subtracted invertible Bloom filters are not compatible.", nameof(subtractedFilterData));
            var countEqualityComparer = EqualityComparer<TCount>.Default;
            var countsIdentity = configuration.CountIdentity();
            for (long i = 0L; i < filterData.Counts.LongLength; i++)
            {
                var filterPure = configuration.IsPureCount(filterData.Counts[i]);
                result.Counts[i] = configuration.CountSubtract(filterData.Counts[i], subtractedFilterData.Counts[i]);
                if (checkHash)
                {
                    result.HashSums[i] = configuration.EntityHashXor(filterData.HashSums[i],
                        subtractedFilterData.HashSums[i]);
                }
                var idXorResult = configuration.IdXor(filterData.IdSums[i], subtractedFilterData.IdSums[i]);
                var resultPure = configuration.IsPureCount(result.Counts[i]);
                var resultZero = countEqualityComparer.Equals(result.Counts[i], countsIdentity);
                if (filterPure &&
                    !resultPure &&
                    !resultZero)
                {
                    listA.Add(filterData.IdSums[i]);
                }
                if (configuration.IsPureCount(subtractedFilterData.Counts[i]) &&
                    resultZero)
                {
                    //pure count went to zero: both filters were pure at the given position.
                    if (!configuration.IsIdIdentity(idXorResult))
                    {
                        listA.Add(filterData.IdSums[i]);
                        listB.Add(subtractedFilterData.IdSums[i]);
                        //set Id to identity, this is no longer a decode error.
                        idXorResult = configuration.IdXor(idXorResult, idXorResult);
                    }
                    else if (checkHash &&
                        !configuration.IsEntityHashIdentity(result.HashSums[i]))
                    {
                        modifiedEntities.Add(subtractedFilterData.IdSums[i]);
                    }
                    if (checkHash)
                    {
                        //any hash sum difference is no longer a decode error
                        result.HashSums[i] = configuration.EntityHashXor(result.HashSums[i], result.HashSums[i]);
                    }
                }
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
            var countEqualityComparer = EqualityComparer<TCount>.Default;
            var countsIdentity = configuration.CountIdentity();
            for (long i = 0L; i < filterData.Counts.LongLength; i++)
            {
                var filterPure = configuration.IsPureCount(filterData.Counts[i]);
                result.Counts[i] = configuration.CountSubtract(filterData.Counts[i], subtractedFilterData.Counts[i]);
                var hashValue = configuration.EntityHashXor(filterData.HashSums[i], subtractedFilterData.HashSums[i]);
                var idXorResult = configuration.IdXor(filterData.IdSums[i], subtractedFilterData.IdSums[i]);
                var resultPure = configuration.IsPureCount(result.Counts[i]);
                var resultZero = countEqualityComparer.Equals(result.Counts[i], countsIdentity);
                if (filterPure &&
                    !resultPure &&
                    !resultZero)
                {
                    modifiedEntities.Add(filterData.HashSums[i]);
                }
                if (configuration.IsPureCount(subtractedFilterData.Counts[i]) &&
                    resultZero)
                {
                    if (!configuration.IsEntityHashIdentity(hashValue))
                    {
                        listA.Add(subtractedFilterData.HashSums[i]);
                        listB.Add(filterData.HashSums[i]);
                    }
                    else if (!configuration.IsIdIdentity(idXorResult))
                    {
                        modifiedEntities.Add(subtractedFilterData.HashSums[i]);
                        if (configuration.IsEntityHashIdentity(hashValue))
                        {
                            //any hash sum difference is no longer a decode error: same Id in the hash, different value. We accounted for this.
                            idXorResult = configuration.IdXor(idXorResult, idXorResult);
                        }
                    }
                    //TODO: when result is not pure, you can't say much: there was a pure item at the position in the subtracted data,
                    //but there were multiple in the other filter.
                    //else if (!resultPure &&
                    //!listA.Contains(filterData.HashSums[i]) &&
                    //!listB.Contains(filterData.HashSums[i]))
                    //{
                    //    modifiedEntities.Add(subtractedFilterData.HashSums[i]);
                    //}
                }
                result.HashSums[i] = hashValue;
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
            var countEqualityComparer = EqualityComparer<TCount>.Default;
            var countComparer = Comparer<TCount>.Default;
            var pureList = new Stack<long>(Range(0L, filter.Counts.LongLength)
                .Where(i => configuration.IsPureCount(filter.Counts[i]))
                .Select(i => i));
            var countsIdentity = configuration.CountIdentity();
            while (pureList.Any())
            {
                var pureIdx = pureList.Pop();
                if (!configuration.IsPureCount(filter.Counts[pureIdx]))
                {
                    continue;
                }
                var id = filter.IdSums[pureIdx];
                if (countComparer.Compare(filter.Counts[pureIdx], countsIdentity) > 0)
                {
                    listA.Add(id);
                }
                else
                {
                    listB.Add(id);
                }
                //no guarantee the hash value is correct, but we can't verify it.
                var hashedValue = filter.HashSums[pureIdx];
                //the difference has been accounted for, zero out.
                filter.Counts[pureIdx] = countsIdentity;
                filter.HashSums[pureIdx] = configuration.EntityHashXor(filter.HashSums[pureIdx], filter.HashSums[pureIdx]);
                filter.IdSums[pureIdx] = configuration.IdXor(id, id);
                foreach (var position in configuration
                    .IdHashes(id, filter.HashFunctionCount)
                    .Select(p => Math.Abs(p % filter.Counts.LongLength))
                    .Where(p => !countEqualityComparer.Equals(filter.Counts[p], countsIdentity)))
                {
                    if (configuration.IsPureCount(filter.Counts[position]))
                    {
                        //we are just about to zero out the count.
                        var hashEquals = configuration.IsEntityHashIdentity(configuration.EntityHashXor(filter.HashSums[position], hashedValue));
                        //pure, hash/identity is different.
                        if (!configuration
                            .IsIdIdentity(configuration.IdXor(id, filter.IdSums[position])) &&
                            hashEquals)
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
                        else if (!hashEquals &&
                                 !listA.Contains(filter.IdSums[position]) &&
                                 !listB.Contains(filter.IdSums[position]))
                        {
                            modifiedEntities.Add(filter.IdSums[position]);
                        }
                    }
                    filter.Remove(configuration, id, hashedValue, position);
                    if (configuration.IsPureCount(filter.Counts[position]))
                    {
                        //count became pure, add to the list.
                        pureList.Push(position);
                    }
                }
            }
            return filter.IsCompleteDecode(configuration, countEqualityComparer, countsIdentity);
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
            var countEqualityComparer = EqualityComparer<TCount>.Default;
            var countComparer = Comparer<TCount>.Default;
            var pureList = new Stack<long>(Range(0L, filter.Counts.LongLength)
                .Where(i => configuration.IsPureCount(filter.Counts[i]))
                .Select(i => i));
            var countsIdentity = configuration.CountIdentity();
            while (pureList.Any())
            {
                var pureIdx = pureList.Pop();
                if (!configuration.IsPureCount(filter.Counts[pureIdx]))
                {
                    continue;
                }
                var id = filter.IdSums[pureIdx];
                var hashSum = filter.HashSums[pureIdx];
                if (countComparer.Compare(filter.Counts[pureIdx], countsIdentity) > 0)
                {
                    listA.Add(hashSum);
                }
                else
                {
                    listB.Add(hashSum);
                }
                //the difference has been accounted for, zero out.
                filter.Counts[pureIdx] = countsIdentity;
                filter.HashSums[pureIdx] = configuration.EntityHashXor(filter.HashSums[pureIdx],
                    filter.HashSums[pureIdx]);
                filter.IdSums[pureIdx] = configuration.IdXor(id, id);
                foreach (var position in configuration
                    .IdHashes(id, filter.HashFunctionCount)
                    .Select(p => Math.Abs(p % filter.Counts.LongLength))
                    .Where(p => !countEqualityComparer.Equals(filter.Counts[p], countsIdentity)))
                {
                    if (configuration.IsPureCount(filter.Counts[position]))
                    {
                        //we are just about to zero out the count.
                        var hashEquals = configuration
                            .IsEntityHashIdentity(configuration.EntityHashXor(
                                filter.HashSums[position],
                                hashSum));
                        //pure, hash/identity is different.
                        if (!configuration
                            .IsIdIdentity(configuration.IdXor(id, filter.IdSums[position])) &&
                            hashEquals)
                        {
                            modifiedEntities.Add(filter.HashSums[position]);
                        }
                        else if (!hashEquals)
                        {
                            listB.Add(filter.HashSums[position]);
                        }
                    }
                    filter.Remove(configuration, id, hashSum, position);
                    if (configuration.IsPureCount(filter.Counts[position]))
                    {
                        //count became pure, add to the list.
                        pureList.Push(position);
                    }
                }
            }
            return filter.IsCompleteDecode(configuration, countEqualityComparer, countsIdentity);
        }

        private static bool IsCompleteDecode<TEntity, TId, TEntityHash, THash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filter,
            IBloomFilterConfiguration<TEntity, TId, TEntityHash, THash, TCount> configuration,
            EqualityComparer<TCount> countEqualityComparer,
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
                   filter.IsReverse ? modifiedEntities : listA,
                    filter.IsReverse ? modifiedEntities : listB,
                    modifiedEntities,
                    destructive);
                foreach (var itm in listA)
                {
                    modifiedEntities.Remove(itm);
                }
                foreach (var itm in listB)
                {
                    modifiedEntities.Remove(itm);
                }
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
        /// <typeparam name="THash"></typeparam>
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
    }
}
