namespace TBag.BloomFilters.Invertible
{
    using BloomFilters.Configurations;
    using Configurations;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    /// <summary>
    /// Extension methods for invertible Bloom filter data.
    /// </summary>
    public static class InvertibleBloomFilterDataExtensions
    {
        /// <summary>
        /// <c>true</c> when the filters are compatible, else <c>false</c>
        /// </summary>
        /// <typeparam name="TId">The type of entity identifier</typeparam>
       /// <typeparam name="TCount">The type of the occurence counter for the invertible Bloom filter.</typeparam>
        /// <typeparam name="TEntity">Type of the entity</typeparam>
        /// <param name="filter">Bloom filter data</param>
        /// <param name="otherFilter">The Bloom filter data to compare against</param>
        /// <param name="configuration">THe Bloom filter configuration</param>
        /// <returns></returns>
        public static bool IsCompatibleWith<TEntity, TId, THash,TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filter,
            IInvertibleBloomFilterData<TId, THash, TCount> otherFilter,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration)
            where TId : struct
             where TCount : struct
            where THash:struct
        {
            if (filter == null || otherFilter == null) return true;
            if (!filter.IsValid() || !otherFilter.IsValid()) return false;
            if (filter.IsReverse != otherFilter.IsReverse ||
               filter.HashFunctionCount != otherFilter.HashFunctionCount ||
                (filter.SubFilter != otherFilter.SubFilter &&
               !filter.SubFilter.IsCompatibleWith(otherFilter.SubFilter, configuration.SubFilterConfiguration)))
                return false;
            if (filter.BlockSize != otherFilter.BlockSize)
            {
                var foldFactors = configuration.FoldingStrategy?.GetFoldFactors(filter.BlockSize, otherFilter.BlockSize);
                if (foldFactors?.Item1 > 1 || foldFactors?.Item2 > 1)
                {
                    return true;
                }
            }
            return filter.BlockSize == otherFilter.BlockSize &&
                   filter.IsReverse == otherFilter.IsReverse &&
                   filter.Counts?.LongLength == otherFilter.Counts?.LongLength;
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
            if (filter == null) return false;
            if (!filter.IsReverse &&
                    (filter.HashSumProvider == null ||
                filter.IdSumProvider == null ||
                filter.Counts == null)) return false;
            return true;
        }     

        /// <summary>
        /// Try to compress the data
        /// </summary>
        /// <typeparam name="TId"></typeparam>
       /// <typeparam name="TCount"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="filterData">The Bloom filter data to compress.</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <returns>The compressed data, or <c>null</c> when compression failed.</returns>
        internal static InvertibleBloomFilterData<TId, THash, TCount> Compress<TEntity, TId, THash,TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filterData,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration)
             where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (filterData == null || configuration?.FoldingStrategy == null) return null;
            var fold = configuration.FoldingStrategy.FindCompressionFactor(filterData.BlockSize, filterData.Capacity, filterData.ItemCount);
            var res = fold.HasValue ? filterData.Fold(configuration, fold.Value) : null;
            if (res == null) return null;
            res.SubFilter = filterData.
                SubFilter
                .Compress(configuration) ?? filterData.SubFilter;
            return res;
        }     

        /// <summary>
        /// Remove an item from the given position.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the Bloom filter occurence count</typeparam>
        /// <param name="filter">The filter</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="idValue">The identifier to remove</param>
        /// <param name="hashValue">The hash value to remove</param>
        /// <param name="position">The position of the cell to remove the identifier and hash from.</param>
        internal static void Remove<TEntity, TId, TCount>(
            this IInvertibleBloomFilterData<TId, int, TCount> filter,
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            TId idValue,
            int hashValue,
            long position)
            where TCount : struct
            where TId : struct
        {
            if (filter == null) return;
            filter.Counts[position] = configuration.CountConfiguration.Decrease(filter.Counts[position]);
            filter.HashSumProvider[position] = configuration.HashXor(filter.HashSumProvider[position], hashValue);
            filter.IdSumProvider[position] = configuration.IdXor(filter.IdSumProvider[position], idValue);
        }

        /// <summary>
        /// Add an item from the given position.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The type of the entity identifier</typeparam>
        /// <typeparam name="TCount">The type of the Bloom filter occurence count</typeparam>
        /// <param name="filter"></param>
        /// <param name="configuration"></param>
        /// <param name="idValue"></param>
        /// <param name="hashValue"></param>
        /// <param name="position"></param>
        internal static void Add<TEntity, TId, TCount>(
            this IInvertibleBloomFilterData<TId, int, TCount> filter,
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            TId idValue,
            int hashValue,
            long position)
            where TCount : struct
            where TId : struct
        {
            if (filter == null) return;
            filter.Counts[position] = configuration.CountConfiguration.Increase(filter.Counts[position]);
            filter.HashSumProvider[position] = configuration.HashXor(filter.HashSumProvider[position], hashValue);
            filter.IdSumProvider[position] = configuration.IdXor(filter.IdSumProvider[position], idValue);
        }

        ///  <summary>
        ///  Add two filters.
        ///  </summary>
        ///  <typeparam name="TEntity">The entity type</typeparam>
        ///  <typeparam name="TId">The type of the entity identifier</typeparam>
        ///  <typeparam name="TCount">The type of the Bloom filter occurence count</typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <param name="filterData"></param>
        ///  <param name="configuration"></param>
        ///  <param name="otherFilterData"></param>
        ///  <param name="inPlace">When <c>true</c> the <paramref name="otherFilterData"/> will be added to the <paramref name="filterData"/> instance, otheerwise a new instance of the filter data will be returned.</param>
        ///  <returns>The filter data or <c>null</c> when the addition failed.</returns>
        /// <remarks></remarks>
        public static IInvertibleBloomFilterData<TId, THash, TCount> Add<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filterData,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
            IInvertibleBloomFilterData<TId, THash, TCount> otherFilterData,
            bool inPlace = true)
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (filterData == null && otherFilterData == null) return null;
            if (filterData == null)
            {
                filterData = otherFilterData.CreateDummy(configuration);
                inPlace = true;
            }
            else
            {
                filterData.SyncCompressionProviders(configuration);
            }
            if (otherFilterData == null)
            {
                otherFilterData = filterData.CreateDummy(configuration);
            }
            else
            {
                otherFilterData.SyncCompressionProviders(configuration);
            }
            if (!filterData.IsCompatibleWith(otherFilterData, configuration)) return null;
            var foldFactors = configuration.FoldingStrategy?.GetFoldFactors(filterData.BlockSize, otherFilterData.BlockSize);
            var res = inPlace && foldFactors?.Item1 <= 1 ?
                filterData :
                (foldFactors == null || foldFactors.Item1 <= 1 ?
                filterData.CreateDummy(configuration) :
                configuration.DataFactory.Create(
                    configuration,
                    filterData.Capacity / foldFactors.Item1,
                    filterData.BlockSize / foldFactors.Item1,
                    filterData.HashFunctionCount));
            res.IsReverse = filterData.IsReverse;
            Parallel.ForEach(
                Partitioner.Create(0L, res.BlockSize),
                (range, state) =>
                {
                    for (var i = range.Item1; i < range.Item2; i++)
                    {
                        res.Counts[i] = configuration.CountConfiguration.Add(
                            filterData.Counts.GetFolded(i, foldFactors?.Item1, configuration.CountConfiguration.Add),
                            otherFilterData.Counts.GetFolded(i, foldFactors?.Item2, configuration.CountConfiguration.Add));
                        res.HashSumProvider[i] = configuration.HashXor(
                          filterData.HashSumProvider.GetFolded(i, filterData.BlockSize, foldFactors?.Item1, configuration.HashXor),
                         otherFilterData.HashSumProvider.GetFolded(i, otherFilterData.BlockSize, foldFactors?.Item2, configuration.HashXor));
                        res.IdSumProvider[i] = configuration.IdXor(
                           filterData.IdSumProvider.GetFolded(i, filterData.BlockSize, foldFactors?.Item1, configuration.IdXor),
                           otherFilterData.IdSumProvider.GetFolded(i, otherFilterData.BlockSize, foldFactors?.Item2, configuration.IdXor));
                    }
                });
            res.SubFilter = filterData
                .SubFilter
                .Add(configuration.SubFilterConfiguration, otherFilterData.SubFilter, inPlace)
                .ConvertToBloomFilterData(configuration);
            res.ItemCount = filterData.ItemCount + otherFilterData.ItemCount;
            return res;
        }

        /// <summary>
        /// Fold the data by the given factor
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <typeparam name="TCount"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <param name="data"></param>
        /// <param name="configuration"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        /// <remarks>Captures the concept of reducing the size of a Bloom filter.</remarks>
        internal static InvertibleBloomFilterData<TId, THash, TCount> Fold<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> data,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
            uint factor)
            where TId : struct
            where TCount : struct
            where THash : struct
        {
            if (factor <= 0)
                throw new ArgumentException($"Fold factor should be a positive number (given value was {factor}.");
            if (data == null) return null;
            if (data.BlockSize % factor != 0)
                throw new ArgumentException($"Bloom filter data cannot be folded by {factor}.", nameof(factor));
            data.SyncCompressionProviders(configuration);
            var res = configuration.DataFactory.Create(
                configuration,
                data.Capacity / factor,
                data.BlockSize / factor,
                data.HashFunctionCount);
            res.IsReverse = data.IsReverse;
            res.ItemCount = data.ItemCount;
            Parallel.ForEach(
                Partitioner.Create(0L, res.BlockSize),
                (range, state) =>
                {
                    for (var i = range.Item1; i < range.Item2; i++)
                    {
                        res.Counts[i] = data.Counts.GetFolded(i, factor, configuration.CountConfiguration.Add);
                        res.HashSumProvider[i] = data.HashSumProvider.GetFolded(i, data.BlockSize, factor, configuration.HashXor);
                        res.IdSumProvider[i] = data.IdSumProvider.GetFolded(i, data.BlockSize, factor, configuration.IdXor);
                    }
                });
            res.SubFilter = data
                .SubFilter?
                .Fold(configuration.SubFilterConfiguration, factor);
            return res;
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
        public static bool? SubtractAndDecode<TEntity, TId, TCount>(
            this IInvertibleBloomFilterData<TId, int, TCount> filter,
            IInvertibleBloomFilterData<TId, int, TCount> subtractedFilter,
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
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
            else
            {
                filter.SyncCompressionProviders(configuration);
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
            else
            {
                subtractedFilter.SyncCompressionProviders(configuration);
            }
            if (!filter.IsCompatibleWith(subtractedFilter, configuration))
                return null;
            bool? valueRes = true;
            var pureList = new Stack<long>();
            var hasSubFilter = filter.SubFilter != null || subtractedFilter.SubFilter != null;
            //add a dummy mod set when there is a reverse filter, because a regular filter is pretty bad at recognizing modified entites.
            var idRes = filter
                .Subtract(subtractedFilter, configuration, listA, listB, pureList, destructive)
                .Decode(configuration, listA, listB, hasSubFilter ? null : modifiedEntities, pureList);
            if (hasSubFilter)
            {
                valueRes = filter
                         .SubFilter
                         .SubtractAndDecode(
                             subtractedFilter.SubFilter,
                             configuration.SubFilterConfiguration,
                             listA,
                             listB,
                             modifiedEntities,
                             destructive);
            }
            if (!valueRes.HasValue || !idRes.HasValue) return null;
            return idRes.Value && valueRes.Value;
        }

        /// <summary>
        /// Convert a <see cref="IInvertibleBloomFilterData{TId, THash, TCount}"/> to a concrete <see cref="InvertibleBloomFilterData{TId, THash, TCount}"/>.
        /// </summary>
        /// <typeparam name="TId">The identifier type</typeparam>
        /// <typeparam name="THash">The entity hash type</typeparam>
        /// <typeparam name="TCount">The occurence count type</typeparam>
        /// <param name="filterData">The IBF data</param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        internal static InvertibleBloomFilterData<TId, THash, TCount> ConvertToBloomFilterData<TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId,THash,TCount> filterData,
            ICountingBloomFilterConfiguration<TId, THash, TCount> configuration)
            where TId : struct
            where TCount : struct
            where THash : struct
        {
            if (filterData == null) return null;
            var result = filterData as InvertibleBloomFilterData<TId, THash, TCount>;
            if (result != null)
            {
                result.SyncCompressionProviders(configuration);
                return result;
            }
            var res = new InvertibleBloomFilterData<TId, THash, TCount>
            {
                HashFunctionCount = filterData.HashFunctionCount,
                BlockSize = filterData.BlockSize,
                HashSums = filterData.HashSumProvider.ToArray(),
                Counts = filterData.Counts,
                IdSums = filterData.IdSumProvider.ToArray(),
                IsReverse = filterData.IsReverse,
                SubFilter = filterData.SubFilter,
                Capacity = filterData.Capacity,
                ItemCount = filterData.ItemCount
            };
            res.SyncCompressionProviders(configuration);
            return res;
        }

        #region Private Methods
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
        private static bool? Decode<TEntity, TId, TCount>(
            this IInvertibleBloomFilterData<TId, int, TCount> filter,
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> configuration,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities = null,
            Stack<long> pureList = null)
            where TId : struct
            where TCount : struct
        {
            if (filter == null) return null;
            var countComparer = Comparer<TCount>.Default;
            if (pureList == null)
            {
                pureList = new Stack<long>(LongEnumerable.Range(0L, filter.BlockSize)
                    .Where(i => configuration.IsPure(filter, i))
                    .Select(i => i));
            }
            var countsIdentity = configuration.CountConfiguration.Identity;
            while (pureList.Any())
            {
                var pureIdx = pureList.Pop();
                if (!configuration.IsPure(filter, pureIdx))
                {
                    continue;
                }
                var id = filter.IdSumProvider[pureIdx];
                var hashSum = filter.HashSumProvider[pureIdx];
                var count = filter.Counts[pureIdx];
                var negCount = countComparer.Compare(count, countsIdentity) < 0;
                var isModified = false;
                foreach (var position in configuration.Probe(filter, hashSum))
                {
                    var wasZero = configuration.CountConfiguration.Comparer.Compare(filter.Counts[position], countsIdentity) == 0;
                    if (configuration.IsPure(filter, position) &&
                        !configuration.HashEqualityComparer.Equals(filter.HashSumProvider[position], hashSum) &&
                        configuration.IdEqualityComparer.Equals(id, filter.IdSumProvider[position]))
                    {
                        modifiedEntities?.Add(id);
                        isModified = true;
                        if (negCount)
                        {
                            filter.Add(configuration, id, filter.HashSumProvider[position], position);
                        }
                        else
                        {
                            filter.Remove(configuration, id, filter.HashSumProvider[position], position);
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
        private static IInvertibleBloomFilterData<TId, THash, TCount> Subtract<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filterData,
            IInvertibleBloomFilterData<TId, THash, TCount> subtractedFilterData,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration,
            HashSet<TId> listA,
            HashSet<TId> listB,
            Stack<long> pureList = null,
            bool destructive = false
            )
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (!filterData.IsCompatibleWith(subtractedFilterData, configuration))
                throw new ArgumentException("Subtracted invertible Bloom filters are not compatible.", nameof(subtractedFilterData));
            var foldFactors = configuration.FoldingStrategy?.GetFoldFactors(filterData.BlockSize, subtractedFilterData.BlockSize);
            if (filterData.BlockSize / (foldFactors?.Item1 ?? 1L) !=
                subtractedFilterData.BlockSize / (foldFactors?.Item2 ?? 1L))
            {
                //failed to find folding factors that will make the size of the filters match.
                return null;
            }
            var result = destructive && foldFactors?.Item1 <= 1 ?
               filterData :
             (foldFactors == null || foldFactors.Item1 <= 1 ?
               filterData.CreateDummy(configuration) :
               configuration.DataFactory.Create(
                   configuration,
                   filterData.Capacity / foldFactors.Item1,
                   filterData.BlockSize / foldFactors.Item1,
                   filterData.HashFunctionCount));
            var idIdentity = configuration.IdIdentity;
            var hashIdentity = configuration.HashIdentity;
            //conccurent place holders
            var listABag = new ConcurrentBag<TId>();
            var listBBag = new ConcurrentBag<TId>();
            var pureListBag = pureList == null ? default(ConcurrentBag<long>) : new ConcurrentBag<long>();
            Parallel.ForEach(
                Partitioner.Create(0L, result.BlockSize),
                (range, state) =>
                {
                    for (var i = range.Item1; i < range.Item2; i++)
                    {
                        var filterCount = filterData.Counts.GetFolded(i, foldFactors?.Item1, configuration.CountConfiguration.Add);
                        var subtractedCount = subtractedFilterData.Counts.GetFolded(i, foldFactors?.Item2, configuration.CountConfiguration.Add);
                        var hashSum = configuration.HashXor(
                           filterData.HashSumProvider.GetFolded(i, filterData.BlockSize, foldFactors?.Item1, configuration.HashXor),
                           subtractedFilterData.HashSumProvider.GetFolded(i, subtractedFilterData.BlockSize, foldFactors?.Item2, configuration.HashXor));
                        var filterIdSum = filterData.IdSumProvider.GetFolded(i, filterData.BlockSize, foldFactors?.Item1, configuration.IdXor);
                        var subtractedIdSum = subtractedFilterData.IdSumProvider.GetFolded(i, subtractedFilterData.BlockSize, foldFactors?.Item2, configuration.IdXor);
                        var idXorResult = configuration.IdXor(filterIdSum, subtractedIdSum);
                        if ((!configuration.IdEqualityComparer.Equals(idIdentity, idXorResult) ||
                            !configuration.HashEqualityComparer.Equals(hashIdentity, hashSum)) &&
                            configuration.CountConfiguration.IsPure(filterCount) &&
                            configuration.CountConfiguration.IsPure(subtractedCount))
                        {
                            //pure count went to zero: both filters were pure at the given position.
                            listABag.Add(filterIdSum);
                            listBBag.Add(subtractedIdSum);
                            idXorResult = idIdentity;
                            hashSum = hashIdentity;
                        }
                        result.Counts[i] = configuration.CountConfiguration.Subtract(filterCount, subtractedCount);
                        result.HashSumProvider[i] = hashSum;
                        result.IdSumProvider[i] = idXorResult;
                        if (configuration.IsPure(result, i))
                        {
                            pureListBag?.Add(i);
                        }
                    }
                });
            //move back to non concurrent data types.
            foreach (var itm in listABag)
            {
                listA.Add(itm);
            }
            foreach (var itm in listBBag)
            {
                listB.Add(itm);
            }
            if (pureList != null)
            {
                foreach (var item in pureListBag)
                {
                    pureList.Push(item);
                }
            }
            result.ItemCount = configuration.CountConfiguration.GetEstimatedCount(result.Counts, result.HashFunctionCount);
            return result;
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
        private static bool IsCompleteDecode<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> filter,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration)
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            var idIdentity = configuration.IdIdentity;
            var hashIdentity = configuration.HashIdentity;
            var countIdentity = configuration.CountConfiguration.Identity;
            var isComplete = 0;
            Parallel.ForEach(
                Partitioner.Create(0L, filter.BlockSize),
                (range, state) =>
                {
                    for (var position = range.Item1; position < range.Item2; position++)
                    {
                        if (configuration.CountConfiguration.IsPure(filter.Counts[position]))
                        {
                            //item is pure and was skipped on purpose.
                            continue;
                        }
                        if (!configuration.IdEqualityComparer.Equals(idIdentity, filter.IdSumProvider[position]) ||
                            !configuration.HashEqualityComparer.Equals(hashIdentity, filter.HashSumProvider[position]) ||
                            configuration.CountConfiguration.Comparer.Compare(filter.Counts[position], countIdentity) != 0)
                        {
                            Interlocked.Increment(ref isComplete);
                            state.Stop();
                        }
                    }
                });
            return isComplete == 0;
        }

        /// <summary>
        /// Duplicate the invertible Bloom filter data
        /// </summary>
        /// <typeparam name="TId">The entity identifier type</typeparam>
        /// <typeparam name="TCount">The occurence count type</typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="THash"></typeparam>
        /// <param name="data">The data to duplicate.</param>
        /// <param name="configuration">The Bloom filter configuration</param>
        /// <returns>Bloom filter data configured the same as <paramref name="data"/>, but with empty arrays.</returns>
        /// <remarks>Explicitly does not duplicate the reverse IBF data.</remarks>
        private static InvertibleBloomFilterData<TId,THash,TCount> CreateDummy<TEntity, TId, THash, TCount>(
            this IInvertibleBloomFilterData<TId, THash, TCount> data,
            IInvertibleBloomFilterConfiguration<TEntity, TId, THash, TCount> configuration)
            where TCount : struct
            where TId : struct
            where THash : struct
        {
            if (data == null) return null;
            var result = configuration.DataFactory.Create(
                configuration,
                data.Capacity, 
                data.BlockSize, 
                data.HashFunctionCount);
            result.IsReverse = data.IsReverse;
            return result;
        }
        #endregion
    }
}
