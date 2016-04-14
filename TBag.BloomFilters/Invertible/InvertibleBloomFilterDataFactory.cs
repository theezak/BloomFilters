using TBag.BloomFilters.Configurations;

namespace TBag.BloomFilters.Invertible
{
    using Configurations;
    using System;

    /// <summary>
    /// Implementation of <see cref="IInvertibleBloomFilterDataFactory"/>.
    /// </summary>
    public class InvertibleBloomFilterDataFactory : IInvertibleBloomFilterDataFactory
    {
        /// <summary>
        /// Extract filter data from the given <paramref name="precalculatedFilter"/> for capacity <paramref name="capacity"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TId">The identifier type</typeparam>
        /// <typeparam name="TCount">The occurence count type</typeparam>
        /// <param name="configuration">Configuration</param>
        /// <param name="precalculatedFilter">The pre-calculated filter</param>
        /// <param name="capacity">The targeted capacity.</param>
        /// <returns>The IBF data sized for <paramref name="precalculatedFilter"/> for target capacity <paramref name="capacity"/>.</returns>
        public IInvertibleBloomFilterData<TId,int,TCount> Extract<TEntity, TId, TCount>(
           IInvertibleBloomFilterConfiguration<TEntity, TId, int,TCount> configuration,
            IInvertibleBloomFilter<TEntity,TId,TCount> precalculatedFilter,
           long? capacity)
           where TCount : struct
           where TId : struct
        {
            if (precalculatedFilter == null) return null;
            if (!capacity.HasValue || capacity < 10)
            {
                //set capacity to arbitrary low capacity.
                capacity = 10;
            }            
            var data = precalculatedFilter.Extract();
            var foldFactor = configuration.FoldingStrategy?.FindCompressionFactor(data.BlockSize, data.Capacity, capacity);
            if (foldFactor > 1)
            {
                return data.Fold(configuration, (uint)foldFactor);
            }           
            return data;
        }

        /// <summary>
        /// Create new Bloom filter data based upon the size and the hash function count.
        /// </summary>
        /// <typeparam name="TId">Type of the identifier</typeparam>
        /// <typeparam name="TCount">Type of the counter</typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="capacity"></param>
        /// <param name="m">Size per hash function</param>
        /// <param name="k">The number of hash functions.</param>
        /// <returns>The Bloom filter data</returns>
        public InvertibleBloomFilterData<TId, THash, TCount> Create<TEntity, TId, THash, TCount>(
            IInvertibleBloomFilterConfiguration<TEntity,TId,THash,TCount>  configuration,
            long capacity, 
            long m, 
            uint k)
            where TId : struct
            where TCount : struct
            where THash : struct
        {
            if (m < 1) // from overflow in bestM calculation
                throw new ArgumentOutOfRangeException(
                    nameof(m),
                    "The provided capacity and errorRate values would result in an array of length > long.MaxValue. Please reduce either the capacity or the error rate.");
            var res = new InvertibleBloomFilterData<TId, THash, TCount>
            {
                HashFunctionCount = k,
                BlockSize = m,
                Counts = new TCount[m],
                Capacity = capacity
            };
            res.SyncCompressionProviders(configuration);
            return res;
        }

        public Type GetDataType<TId, THash, TCount>()
            where TId : struct
            where THash : struct
            where TCount : struct
        {
            return typeof(InvertibleBloomFilterData<TId, THash, TCount>);
        }

    }
}
