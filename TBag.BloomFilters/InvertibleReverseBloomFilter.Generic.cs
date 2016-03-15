namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// An invertible Bloom filter supports removal and additions.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TCount"></typeparam>
    public class InvertibleReverseBloomFilter<TEntity, TId,TCount> : 
        InvertibleBloomFilter<TEntity, TId,TCount>
        where TCount : struct
        where TId : struct
    {
        #region Fields
        private InvertibleBloomFilterData<int, TId, TCount> _data;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Bloom filter using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        public InvertibleReverseBloomFilter(
            long capacity, 
            IBloomFilterConfiguration<TEntity,TId,int, int, TCount> bloomFilterConfiguration) : this(
                capacity,
                bloomFilterConfiguration.BestErrorRate(capacity), 
                bloomFilterConfiguration) { }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        public InvertibleReverseBloomFilter(
            long capacity, 
            float errorRate, 
            IBloomFilterConfiguration<TEntity,TId,int, int, TCount> bloomFilterConfiguration) : this(
                capacity,
                bloomFilterConfiguration.BestCompressedSize(capacity, errorRate),
                bloomFilterConfiguration.BestHashFunctionCount(capacity, errorRate),
                  bloomFilterConfiguration)
        { }

        /// <summary>
        /// Creates a new Bloom filter.
        /// </summary>
        /// <param name="capacity">The capacity (typically the size ofthe set to add)</param>
        /// <param name="m">The size of the Bloom filter</param>
        /// <param name="k">The number of hash functions to use.</param>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration.</param>
        public InvertibleReverseBloomFilter(
            long capacity,
            long m,
            uint k,
            IBloomFilterConfiguration<TEntity, TId, int, int, TCount> bloomFilterConfiguration) : 
            base(capacity, m, k, bloomFilterConfiguration)
        {
            _data = Extract().Reverse();
        }
        #endregion

        #region Implementation of Bloom Filter public contract

        /// <summary>
        /// Add an item to the Bloom filter.
        /// </summary>
        /// <param name="item"></param>
        public override void Add(TEntity item)
        {
           var id = Configuration.ValueFilterConfiguration.GetId(item);
            var hashValue = Configuration.ValueFilterConfiguration.EntityHashes(item, 1).First();
            foreach (var position in Configuration
               .ValueFilterConfiguration
               .IdHashes(id, _data.HashFunctionCount)
               .Select(p => Math.Abs(p % _data.Counts.LongLength)))
            {
                _data.Counts[position] = Configuration
                    .ValueFilterConfiguration
                    .CountIncrease(_data.Counts[position]);
                _data.IdSums[position] = Configuration
                    .ValueFilterConfiguration
                    .IdXor(_data.IdSums[position], id);
                _data.HashSums[position] = Configuration
                    .ValueFilterConfiguration
                    .EntityHashXor(_data.HashSums[position], hashValue);
            }
        }

        public override void Rehydrate(IInvertibleBloomFilterData<TId, int, TCount> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.IsReverse)
                throw new ArgumentException("An invertible reverse Bloom filter does not accept data for a regular invertible Bloom filter. Please use an invertible Bloom filter.", nameof(data));
            if (!data.IsValid())
                throw new ArgumentException(
                    "Invertible reverse Bloom filter data is invalid.",
                    nameof(data));
            _data = data.Reverse();
        }

        /// <summary>
        /// Determine if the item is in the Bloom filter with the same value.
        /// </summary>
        /// <param name="item"></param>
        /// <returns><c>false</c> when both the identifier and the hash value can be found in the Bloom filter, else <c>true</c></returns>
        /// <remarks></remarks>
        public override bool Contains(TEntity item)
        {
            var valueId = Configuration.ValueFilterConfiguration.GetId(item);
            var hashValue = Configuration.ValueFilterConfiguration.EntityHashes(item, 1).First();
            var countIdentity = Configuration.ValueFilterConfiguration.CountIdentity();
            foreach (var position in Configuration
                .ValueFilterConfiguration
                .IdHashes(valueId, _data.HashFunctionCount)
                .Select(p => Math.Abs(p % _data.Counts.LongLength)))
            {
                if (Configuration.ValueFilterConfiguration.IsPure(_data, position) &&
                    (!Configuration
                        .ValueFilterConfiguration
                        .IdEqualityComparer.Equals(_data.IdSums[position], valueId) ||
                        !Configuration
                        .ValueFilterConfiguration
                        .EntityHashEqualityComparer.Equals(_data.HashSums[position], hashValue)))
                    {
                    return false;
                }
                else if (Configuration.CountEqualityComparer.Equals(_data.Counts[position], countIdentity))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Remove the given item from the Bloom filter.
        /// </summary>
        /// <param name="item"></param>
        public override void Remove(TEntity item)
        {
            var id = Configuration.ValueFilterConfiguration.GetId(item);
            var hashValue = Configuration.ValueFilterConfiguration.EntityHashes(item, 1).First();
            foreach (var position in Configuration
                .ValueFilterConfiguration
                .IdHashes(id, _data.HashFunctionCount)
                .Select(p => Math.Abs(p % _data.Counts.LongLength)))
            {
                _data
                    .Remove(Configuration.ValueFilterConfiguration, id, hashValue, position);
            }
        }

        public override bool ContainsKey(TId key)
        {
            throw new NotImplementedException("ContainsKey is not supported on invertible reverse Bloom filters.");
        }

        /// <summary>
        /// Subtract and then decode.
        /// </summary>
        /// <param name="filter">Bloom filter to subtract</param>
        /// <param name="listA">Items in this filter, but not in <paramref name="filter"/></param>
        /// <param name="listB">Items not in this filter, but in <paramref name="filter"/></param>
        /// <param name="modifiedEntities">Entities in both filters, but with a different value</param>
        /// <returns><c>true</c> when the decode was successful, otherwise <c>false</c></returns>
        public override bool SubtractAndDecode(IInvertibleBloomFilterData<TId, int, TCount> filter,
            HashSet<TId> listA,
            HashSet<TId> listB,
            HashSet<TId> modifiedEntities)
        {
            return _data.HashSubtractAndDecode(
                filter.Reverse(),
                Configuration.ValueFilterConfiguration,
                listA,
                listB,
                modifiedEntities);
        }

        #endregion
    }
}