namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
  
    /// <summary>
    /// An invertible Bloom filter supports removal and additions.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TCount"></typeparam>
    /// <remarks>Contains both a regular invertible Bloom filter and an invertible reverse Bloom filter.</remarks>
    public class InvertibleHybridBloomFilter<TEntity, TId,TCount> : 
        InvertibleBloomFilter<TEntity, TId,TCount>
        where TCount : struct
        where TId : struct
    {
        #region Fields
        private readonly InvertibleReverseBloomFilter<TEntity, TId, TCount> _reverseBloomFilter;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Bloom filter using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="capacity">The anticipated number of items to be added to the filter. More than this number of items can be added, but the error rate will exceed what is expected.</param>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        public InvertibleHybridBloomFilter(
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
        public InvertibleHybridBloomFilter(
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
        public InvertibleHybridBloomFilter(
            long capacity,
            long m,
            uint k,
            IBloomFilterConfiguration<TEntity, TId, int, int, TCount> bloomFilterConfiguration) : base(capacity, m, k, bloomFilterConfiguration)
        {
            _reverseBloomFilter = new InvertibleReverseBloomFilter<TEntity, TId, TCount>(capacity, m, k, bloomFilterConfiguration);
            Extract().ValueFilter = _reverseBloomFilter.Extract().Reverse();
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
            _reverseBloomFilter.Add(item);
        }

        /// <summary>
        /// Remove the given item from the Bloom filter.
        /// </summary>
        /// <param name="item"></param>
        public override void Remove(TEntity item)
        {
            base.Remove(item);
            _reverseBloomFilter.Remove(item);
        }

        public override void Rehydrate(IInvertibleBloomFilterData<TId, int, TCount> data)
        {
            if (data == null || 
                data.ValueFilter == null)
                throw new ArgumentException("Data and value filter data are required for a hybrid estimator.", nameof(data));
            base.Rehydrate(data);
            _reverseBloomFilter.Rehydrate(data.ValueFilter.Reverse());
        }

        public override bool Contains(TEntity item)
        {
            return base.Contains(item) &&
                _reverseBloomFilter.Contains(item);
        }

        #endregion
    }
}