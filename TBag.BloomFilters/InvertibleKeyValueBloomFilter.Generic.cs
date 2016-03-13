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
    /// <remarks>TODO: recognize that more is needed to handle differences in values!!!!!!!!!!!!!!!
    /// Proposal: remove current value hashes as they currently function.
    /// THEN: add reversal of how it currently works. Value hash => ID , and ID => value. This will decode value changes as well, and provide you the ID to go with it!
    /// </remarks>
    public class InvertibleKeyValueBloomFilter<TEntity, TId,TCount> : InvertibleBloomFilter<TEntity, TId,TCount>
        where TCount : struct
        where TId : struct
    {
        #region Fields
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Bloom filter using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        public InvertibleKeyValueBloomFilter(
            long capacity, 
            IBloomFilterConfiguration<TEntity,TId,int, int, TCount> bloomFilterConfiguration) : this(
                capacity, 
                BestErrorRate(capacity), 
                bloomFilterConfiguration) { }

        /// <summary>
        /// Creates a new Bloom filter, using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="errorRate">The accepable false-positive rate (e.g., 0.01F = 1%)</param>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        public InvertibleKeyValueBloomFilter(
            long capacity, 
            float errorRate, 
            IBloomFilterConfiguration<TEntity,TId,int, int, TCount> bloomFilterConfiguration) : this(
                capacity, 
                BestCompressedM(capacity, errorRate), 
                BestK(capacity, errorRate),
                  bloomFilterConfiguration)
        { }

        /// <summary>
        /// Creates a new Bloom filter.
        /// </summary>
        /// <param name="capacity">The capacity (typically the size ofthe set to add)</param>
        /// <param name="m">The size of the Bloom filter</param>
        /// <param name="k">The number of hash functions to use.</param>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration.</param>
        public InvertibleKeyValueBloomFilter(
            long capacity,          
            long m,
            uint k,
            IBloomFilterConfiguration<TEntity,TId, int, int, TCount> bloomFilterConfiguration) : base(capacity, m, k, bloomFilterConfiguration)
        {
            var data = Extract();
            data.ValueFilter = new InvertibleBloomFilterData<int, TId, TCount>
            {
                BlockSize = data.BlockSize,
                Counts = new TCount[data.Counts.LongLength],
                HashFunctionCount = data.HashFunctionCount,
                IdSums = new int[data.Counts.LongLength],
                HashSums = new TId[data.Counts.LongLength]
            };                       
        }
        #endregion

        #region Implementation of Bloom Filter public contract

     /// <summary>
     /// Add an item to the Bloom filter.
     /// </summary>
     /// <param name="item"></param>
        public override void Add(TEntity item)
        {
            base.Add(item);
            var id = Configuration.ValueFilterConfiguration.GetId(item);
         var hashValue = Configuration.ValueFilterConfiguration.EntityHashes(item, 1).First();
             foreach (var position in Configuration
                .ValueFilterConfiguration
                .IdHashes(id, Data.ValueFilter.HashFunctionCount)
                .Select(p =>Math.Abs(p % Data.ValueFilter.Counts.LongLength)))
            {
                Data.ValueFilter.Counts[position] = Configuration
                    .ValueFilterConfiguration
                    .CountIncrease(Data.ValueFilter.Counts[position]);
                Data.ValueFilter.IdSums[position] = Configuration
                    .ValueFilterConfiguration
                    .IdXor(Data.ValueFilter.IdSums[position], id);
                Data.ValueFilter.HashSums[position] = Configuration
                    .ValueFilterConfiguration
                    .EntityHashXor(Data.ValueFilter.HashSums[position], hashValue);
              }
        }

        /// <summary>
        /// Determine if the item is in the Bloom filter with the same value.
        /// </summary>
        /// <param name="item"></param>
        /// <returns><c>false</c> when both the identifier and the hash value can be found in the Bloom filter, else <c>true</c></returns>
        /// <remarks></remarks>
        public bool ContainsUmodified(TEntity item)
        {
            var containsId = Contains(item);
            if (!containsId) return true;
            var valueId = Configuration.ValueFilterConfiguration.GetId(item);
            var countIdentity = Configuration.ValueFilterConfiguration.CountIdentity();
            foreach (var position in Configuration
                .ValueFilterConfiguration
                .IdHashes(valueId, Data.ValueFilter.HashFunctionCount)
                .Select(p => Math.Abs(p % Data.ValueFilter.Counts.LongLength)))
            {
                if (IsPure(Configuration.ValueFilterConfiguration, Data.ValueFilter, position))
                {
                    if (!Configuration
                        .ValueFilterConfiguration
                        .IsIdIdentity(Configuration.ValueFilterConfiguration.IdXor(Data.ValueFilter.IdSums[position], valueId)))
                    {
                        return false;
                    }
                }
                else if (CountEqualityComparer.Equals(Data.ValueFilter.Counts[position], countIdentity))
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
            base.Remove(item);
            var id = Configuration.ValueFilterConfiguration.GetId(item);
            var hashValue = Configuration.ValueFilterConfiguration.EntityHashes(item, 1).First();
            foreach (var position in Configuration
                .ValueFilterConfiguration
                .IdHashes(id, Data.ValueFilter.HashFunctionCount)
                .Select(p =>Math.Abs(p % Data.ValueFilter.Counts.LongLength)))
            {
                Data
                    .ValueFilter
                    .Remove(Configuration.ValueFilterConfiguration, id, hashValue, position);
            }
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
            var idDecode = base.SubtractAndDecode(
                filter, 
                listA, 
                listB, 
                modifiedEntities);
            var valueDecode = Data.ValueFilter.HashSubtractAndDecode(
                filter.ValueFilter, 
                Configuration.ValueFilterConfiguration, 
                listA, 
                listB, 
                modifiedEntities);
            return idDecode && valueDecode;
        }

        #endregion
    }
}