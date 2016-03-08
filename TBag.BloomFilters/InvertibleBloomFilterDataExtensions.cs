namespace TBag.BloomFilters
{
    public static class InvertibleBloomFilterDataExtensions
    {
        /// <summary>
        /// <c>true</c> when the filters are compatible, else <c>false</c>
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <param name="filter"></param>
        /// <param name="otherFilter"></param>
        /// <returns></returns>
        public static bool IsCompatibleWith<TId,TCount>(this IInvertibleBloomFilterData<TId, TCount> filter, IInvertibleBloomFilterData<TId,TCount> otherFilter)
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
        /// <param name="filter"></param>
        /// <returns></returns>
        public static bool IsValid<TId,TCount>(this IInvertibleBloomFilterData<TId,TCount> filter)
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
        /// <param name="filter"></param>
        /// <returns></returns>
        public static bool HasRows<TId,TCount>(this IInvertibleBloomFilterData<TId,TCount> filter)
        {
            if (filter == null || filter.Counts == null) return false;
            return filter.BlockSize != filter.Counts.LongLength;
        }
    }
}
