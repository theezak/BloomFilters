namespace TBag.BloomFilters
{
    using Configurations;
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
        /// <param name="filter">Bloom filter data</param>
        /// <param name="otherFilter">The Bloom filter data to compare against</param>
        /// <returns></returns>
        public static bool IsCompatibleWith<TId, TEntityHash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filter,
            IInvertibleBloomFilterData<TId, TEntityHash, TCount> otherFilter)
            where TId : struct
            where TEntityHash : struct
            where TCount : struct
        {
            if (filter == null || otherFilter == null) return true;
            if (!filter.IsValid() || !otherFilter.IsValid()) return false;
            return filter.BlockSize == otherFilter.BlockSize &&
                filter.IsReverse == otherFilter.IsReverse &&
                filter.SubFilterCount == otherFilter.SubFilterCount &
               filter.HashFunctionCount == otherFilter.HashFunctionCount &&
               filter.Counts?.LongLength == otherFilter.Counts?.LongLength &&
               filter.HashSums?.LongLength == otherFilter.HashSums?.LongLength &&
               filter.IdSums?.LongLength == otherFilter.IdSums?.LongLength &&
               (filter.SubFilters == otherFilter.SubFilters ||
               filter.SubFilters.IsCompatibleWith(otherFilter.SubFilters));
        }

        private static bool IsCompatibleWith<TId, TEntityHash, TCount>(
           this IInvertibleBloomFilterData<TId, TEntityHash, TCount>[] filter,
           IInvertibleBloomFilterData<TId, TEntityHash, TCount>[] otherFilter)
           where TId : struct
           where TEntityHash : struct
           where TCount : struct
        {
            if (filter == otherFilter) return true;
            if (filter.Length > 0 && otherFilter.Length > 0)
                return filter[0].IsCompatibleWith(otherFilter[0]);
            return true;
        }

        /// <summary>
        /// <c>true</c> when the filter is valid, else <c>false</c>.
        /// </summary>
        /// <typeparam name="TId">The type of entity identifier</typeparam>
        /// <typeparam name="TEntityHash">The type of the entity hash.</typeparam>
        /// <typeparam name="TCount">The type of the occurence counter for the invertible Bloom filter.</typeparam>
        /// <param name="filter">The Bloom filter data to validate.</param>
        /// <returns></returns>
        public static bool IsValid<TId, TEntityHash, TCount>(this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filter)
            where TCount : struct
            where TEntityHash : struct
            where TId : struct
        {
            if (!filter.IsReverse &&
                    (filter?.Counts == null ||
                filter.IdSums == null ||
                filter.HashSums == null)) return false;
            if (filter.Counts?.LongLength != filter.HashSums?.LongLength ||
                filter.Counts?.LongLength != filter.IdSums?.LongLength ||
               filter.BlockSize != (filter.Counts?.LongLength??filter.BlockSize)) return false;
            return true;
        }

        /// <summary>
        /// Subtract the Bloom filter data.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <typeparam name="TCount">The occurence count type</typeparam>
        /// <typeparam name="THash">The hash type.</typeparam>
        /// <param name="filterData">The filter data</param>
        /// <param name="subtractedFilterData">The Bloom filter data to subtract</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="listA">Items in <paramref name="filterData"/>, but not in <paramref name="subtractedFilterData"/></param>
        /// <param name="listB">Items in <paramref name="subtractedFilterData"/>, but not in <paramref name="filterData"/></param>
        /// <param name="pureList">Optional list of pure items.</param>
        /// <param name="destructive">When <c>true</c> the <paramref name="filterData"/> will no longer be valid after the subtract operation, otherwise <c>false</c></param>
        /// <returns>The resulting Bloom filter data</returns>
        internal static IInvertibleBloomFilterData<TId, THash, TCount> Subtract<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filterData,
            IInvertibleBloomFilterData<TId, THash, TCount> subtractedFilterData,
            IBloomFilterConfiguration<TEntity, TId,  THash, TCount> configuration,
            HashSet<TId> listA,
            HashSet<TId> listB,
            Stack<long> pureList = null,
            bool destructive = false
            )
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (!filterData.IsCompatibleWith(subtractedFilterData))
                throw new ArgumentException("Subtracted invertible Bloom filters are not compatible.", nameof(subtractedFilterData));
            var result = destructive ?
                filterData :
                filterData.CreateDummy(configuration);
             var idIdentity = configuration.IdIdentity();
            var hashIdentity = configuration.HashIdentity();
            for (var i = 0L; i < filterData.Counts.LongLength; i++)
            {
                 var hashSum = configuration.HashXor(
                    filterData.HashSums[i],
                    subtractedFilterData.HashSums[i]);
                var idXorResult = configuration.IdXor(filterData.IdSums[i], subtractedFilterData.IdSums[i]);                
                if ((!configuration.IdEqualityComparer.Equals(idIdentity, idXorResult) ||
                    !configuration.HashEqualityComparer.Equals(hashIdentity, hashSum)) &&
                    configuration.IsPure(subtractedFilterData, i) &&
                    configuration.IsPure(filterData, i))
                {
                    //pure count went to zero: both filters were pure at the given position.
                    listA.Add(filterData.IdSums[i]);
                    listB.Add(subtractedFilterData.IdSums[i]);
                    idXorResult = idIdentity;
                    hashSum = hashIdentity;
                }
                result.Counts[i] = configuration.CountConfiguration.CountSubtract(filterData.Counts[i], subtractedFilterData.Counts[i]);
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
        /// <param name="pureList">Optional list of pure items</param>
        /// <returns><c>true</c> when the decode was successful, else <c>false</c>.</returns>
        internal static bool Decode<TEntity, TId, TCount>(
            this IInvertibleBloomFilterData<TId, int, TCount> filter,
            IBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities = null,
            Stack<long> pureList = null)
            where TId : struct
            where TCount : struct
        {
            var countComparer = Comparer<TCount>.Default;
            if (pureList == null)
            {
                pureList = new Stack<long>(LongEnumerable.Range(0L, filter.Counts.LongLength)
                    .Where(i => configuration.IsPure(filter, i))
                    .Select(i => i));
            }
            var countsIdentity = configuration.CountConfiguration.CountIdentity();
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
                foreach (var position in configuration.Probe(filter, hashSum))
                 {
                    var wasZero = configuration.CountConfiguration.EqualityComparer.Equals(filter.Counts[position], countsIdentity);
                    if (configuration.IsPure(filter, position) &&
                        !configuration.HashEqualityComparer.Equals(filter.HashSums[position], hashSum) &&
                        configuration.IdEqualityComparer.Equals(id, filter.IdSums[position]))
                    {
                        modifiedEntities?.Add(id);
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
                    if (!wasZero && configuration.IsPure(filter, position))
                    {
                        //count became pure, add to the list.
                        pureList.Push(position);
                    }
                }
                if (isModified) continue;
                if (negCount)
                {
                    listB.Add(id);
                }
                else
                {
                    listA.Add(id);
                }
            }
            modifiedEntities?.MoveModified(listA, listB);
            return filter.IsCompleteDecode(configuration);
        }

        /// <summary>
        /// Determine if the decode succeeded.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The identifier type</typeparam>
        /// <typeparam name="THash">The type of the hash</typeparam>
        /// <typeparam name="TCount">The type of the occurence counter</typeparam>
        /// <param name="filter">The IBF data</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <returns><c>true</c> when the decode was successful, else <c>false</c>.</returns>
        internal static bool IsCompleteDecode<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filter,
            IBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration)
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            var idIdentity = configuration.IdIdentity();
            var hashIdentity = configuration.HashIdentity();
            var countIdentity = configuration.CountConfiguration.CountIdentity();
            for (var position = 0L; position < filter.Counts.LongLength; position++)
            {
                if (configuration.CountConfiguration.IsPureCount(filter.Counts[position]))
                {
                    //item is pure and was skipped on purpose.
                    continue;
                }
                if (!configuration.IdEqualityComparer.Equals(idIdentity, filter.IdSums[position]) ||                   
                    !configuration.HashEqualityComparer.Equals(hashIdentity, filter.HashSums[position]) ||
                    !configuration.CountConfiguration.EqualityComparer.Equals(filter.Counts[position], countIdentity))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Duplicate the invertible Bloom filter data
        /// </summary>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <typeparam name="THash">The entity hash type</typeparam>
        /// <typeparam name="TCount">The occurence count type</typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="data">The data to duplicate.</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <returns>Bloom filter data configured the same as <paramref name="data"/>, but with empty arrays.</returns>
        /// <remarks>Explicitly does not duplicate the reverse IBF data.</remarks>
        private static InvertibleBloomFilterData<TId,THash,TCount> CreateDummy<TEntity, TId,THash,TCount>(
            this IInvertibleBloomFilterData<TId,THash,TCount> data,
            IBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration)
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (data == null) return null;
            var result = configuration.DataFactory.Create<TId, THash, TCount>(data.BlockSize, data.HashFunctionCount);
            result.IsReverse = data.IsReverse;
            result.SubFilterCount = data.SubFilterCount;
            return result;
        }

        /// <summary>
        /// Remove an item from the given position.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="THash">The type of the hash</typeparam>
        /// <typeparam name="TCount">The type of the Bloom filter occurence count</typeparam>
        /// <param name="filter">The filter</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="idValue">The identifier to remove</param>
        /// <param name="hashValue">The hash value to remove</param>
        /// <param name="position">The position of the cell to remove the identifier and hash from.</param>
        internal static void Remove<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filter,
            IBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
            TId idValue,
            THash hashValue,
            long position)
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (filter == null) return;
            filter.Counts[position] = configuration.CountConfiguration.CountDecrease(filter.Counts[position]);
            filter.IdSums[position] = configuration.IdXor(filter.IdSums[position], idValue);
            filter.HashSums[position] = configuration.HashXor(filter.HashSums[position], hashValue);
        }

        /// <summary>
        /// Add an item from the given position.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="THash">The type of the hash</typeparam>
        /// <typeparam name="TCount">The type of the Bloom filter occurence count</typeparam>
        /// <param name="filter"></param>
        /// <param name="configuration"></param>
        /// <param name="idValue"></param>
        /// <param name="hashValue"></param>
        /// <param name="position"></param>
        internal static void Add<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filter,
            IBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
            TId idValue,
            THash hashValue,
            long position)
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (filter == null) return;
            filter.Counts[position] = configuration.CountConfiguration.CountIncrease(filter.Counts[position]);
            filter.IdSums[position] = configuration.IdXor(filter.IdSums[position], idValue);
            filter.HashSums[position] = configuration.HashXor(filter.HashSums[position], hashValue);
        }

        /// <summary>
        /// Subtract the given filter and decode for any changes
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the Bloom filter occurence count</typeparam>
        /// <param name="filter">Filter</param>
        /// <param name="subtractedFilter">The Bloom filter to subtract</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <param name="listA">Items in <paramref name="filter"/>, but not in <paramref name="subtractedFilter"/></param>
        /// <param name="listB">Items in <paramref name="subtractedFilter"/>, but not in <paramref name="filter"/></param>
        /// <param name="modifiedEntities">items in both filters, but with a different value.</param>
        /// <param name="destructive">Optional parameter, when <c>true</c> the filter <paramref name="filter"/> will be modified, and thus rendered useless, by the decoding.</param>
        /// <returns><c>true</c> when the decode was successful, else <c>false</c>.</returns>
        public static bool SubtractAndDecode<TEntity, TId, TCount>(
            this IInvertibleBloomFilterData<TId, int, TCount> filter,
            IInvertibleBloomFilterData<TId, int, TCount> subtractedFilter,
            IBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities,
            bool destructive = false)
            where TId : struct
            where TCount : struct
        {
            if (filter == null && subtractedFilter == null) return true;
            if (filter == null)
            {
                //handle null filters as elegant as possible at this point.
                filter = subtractedFilter.CreateDummy(configuration);
                destructive = true;
            }
            if (subtractedFilter == null)
            {
                //swap the filters and the sets so we can still apply the destructive setting to temporarily created filter data 
                subtractedFilter = filter;
                filter = subtractedFilter.CreateDummy(configuration);
                var swap = listA;
                listA = listB;
                listB = swap;
                destructive = true;
            }
            if (!filter.IsCompatibleWith(subtractedFilter))
                throw new ArgumentException(
                    "The subtracted Bloom filter data is not compatible with the Bloom filter.",
                    nameof(subtractedFilter));
            var valueRes = true;
            var pureList = new Stack<long>();
            var hasReverseFilter = filter.SubFilterCount > 0 || subtractedFilter.SubFilterCount > 0;
            //add a dummy mod set when there is a reverse filter, because a regular filter is pretty bad at recognizing modified entites.
            var idRes = filter.Counts==null && filter.IsReverse || filter
                .Subtract(subtractedFilter, configuration, listA, listB, pureList, destructive)
                .Decode(configuration, listA, listB, hasReverseFilter ? null : modifiedEntities, pureList);
            if (hasReverseFilter)
            {
                if (!filter.IsReverse || filter.SubFilterCount == 1)
                {
                    valueRes = filter
                        .GetSubFilter(0)
                        .SubtractAndDecode(
                            subtractedFilter.GetSubFilter(0),
                            configuration.SubFilterConfiguration,
                            listA,
                            listB,
                            modifiedEntities,
                            destructive);
                }
                else
                {
                    valueRes = ParallelSubFilterDecode(filter, subtractedFilter, configuration, listA, listB, modifiedEntities, destructive);
                }
            }
            return idRes && valueRes;
        }

        /// <summary>
        /// Decode sub filters in parallel.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="filter"></param>
        /// <param name="subtractedFilter"></param>
        /// <param name="configuration"></param>
        /// <param name="listA"></param>
        /// <param name="listB"></param>
        /// <param name="modifiedEntities"></param>
        /// <param name="destructive"></param>
        /// <returns></returns>
        private static bool ParallelSubFilterDecode<TEntity, TId, TCount>(
            IInvertibleBloomFilterData<TId, int, TCount> filter,
            IInvertibleBloomFilterData<TId, int, TCount> subtractedFilter, 
            IBloomFilterConfiguration<TEntity, TId, int, TCount> configuration, 
            HashSet<TId> listA, 
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities, 
            bool destructive) where TId : struct where TCount : struct
        {
            var subFilterCount = Math.Max(filter.SubFilterCount, subtractedFilter.SubFilterCount);
            var res = new Tuple<HashSet<TId>, HashSet<TId>, HashSet<TId>, bool>[subFilterCount];
            LongEnumerable.Range(0L, subFilterCount)
                .AsParallel()
                .ForAll(i =>
                {
                    var h1 = new HashSet<TId>();
                    var h2 = new HashSet<TId>();
                    var h3 = new HashSet<TId>();
                    res[i] = new Tuple<HashSet<TId>, HashSet<TId>, HashSet<TId>, bool>(h1, h2, h3, filter.GetSubFilter(i)
                        .SubtractAndDecode(
                            subtractedFilter.GetSubFilter(i),
                            configuration.SubFilterConfiguration,
                            h1,
                            h2,
                            h3,
                            destructive));
                });
           foreach (var r in res)
            {
                foreach (var item in r.Item1)
                {
                    listA.Add(item);
                }
                foreach (var item in r.Item2)
                {
                    listB.Add(item);
                }
                foreach (var item in r.Item3)
                {
                    modifiedEntities.Add(item);
                }
            }
            return res.All(r => r.Item4);
        }

        /// <summary>
        /// Convert a <see cref="IInvertibleBloomFilterData{TId, TEntityHash, TCount}"/> to a concrete <see cref="InvertibleBloomFilterData{TId, TEntityHash, TCount}"/>.
        /// </summary>
        /// <typeparam name="TId">The identifier type</typeparam>
        /// <typeparam name="TEntityHash">The entity hash type</typeparam>
        /// <typeparam name="TCount">The occurence count type</typeparam>
        /// <param name="filterData">The IBF data</param>
        /// <returns></returns>
        internal static InvertibleBloomFilterData<TId, TEntityHash, TCount> ConvertToBloomFilterData<TEntity, TId, TEntityHash, TCount>(
            this IInvertibleBloomFilterData<TId, TEntityHash, TCount> filterData,
            IBloomFilterConfiguration<TEntity, TId, TEntityHash, TCount> configuration)
            where TId : struct
            where TEntityHash : struct
            where TCount : struct
        {
            if (filterData == null) return null;
            var result = filterData as InvertibleBloomFilterData<TId, TEntityHash, TCount>;
            if (result != null) return result;
            return new InvertibleBloomFilterData<TId, TEntityHash, TCount>
            {
                HashFunctionCount = filterData.HashFunctionCount,
                BlockSize = filterData.BlockSize,
                Counts = filterData.Counts,
                HashSums = filterData.HashSums,
                IdSums = filterData.IdSums,
                IsReverse = filterData.IsReverse,
                SubFilterIndexes = filterData.SubFilterIndexes,
                SubFilters = filterData.SubFilters,
                SubFilterCount = filterData.SubFilterCount
            };
        }

        /// <summary>
        /// Get the sub filter at the given index.
        /// </summary>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the counter</typeparam>
        /// <param name="data">The Bloom filter data</param>
        /// <param name="index">The index for the sub filter.</param>
        /// <returns></returns>
        internal static IInvertibleBloomFilterData<TId,int,TCount> GetSubFilter<TId, TCount>(this IInvertibleBloomFilterData<TId, int, TCount> data, long index)
            where TId : struct
            where TCount : struct
        {
            if (data == null) return null;
            if (data.SubFilterIndexes==null)
            {
                if (data.SubFilters?.Length > index)
                {
                    return data.SubFilters?[index];
                }
                return null;
            }
            for(var j=0; j < data.SubFilterIndexes.Length; j++)
            {
                if (data.SubFilterIndexes[j] == index)
                {
                    return data.SubFilters[j];
                }
            }
            return null;
        }

        /// <summary>
        /// Get the size ofthe filter.
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static long GetFilterBlockSize<TId, TCount>(this IInvertibleBloomFilterData<TId, int, TCount> data)
           where TId : struct
           where TCount : struct
        {
            if (data == null) return 0L;
            if (data.IsReverse && data.SubFilters?.Length > 0)
            {
                return data.SubFilters[0].BlockSize;
            }
            return data.BlockSize;
        }

        }
}
