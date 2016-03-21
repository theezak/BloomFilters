namespace TBag.BloomFilters
{
    using System;
    using System.Collections.Generic;
    /// <summary>
    /// An invertible Bloom filter supports removal and additions.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    /// <typeparam name="TId">The type of the entity identifier</typeparam>
    /// <typeparam name="TCount">The type of the occurence count in the Bloom filter.</typeparam>
    /// <remarks>Contains both a regular invertible Bloom filter and a reverse invertible Bloom filter.</remarks>
    public class InvertibleHybridBloomFilter<TEntity, TId,TCount> : 
        InvertibleBloomFilter<TEntity, TId,TCount>
        where TCount : struct
        where TId : struct
    {
        #region Fields
        private readonly IInvertibleBloomFilter<KeyValuePair<TId,int>, TId,  TCount> _reverseBloomFilter;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new Bloom filter using the optimal size for the underlying data structure based on the desired capacity and error rate, as well as the optimal number of hash functions.
        /// </summary>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        /// <param name="invertibleBloomFilterDataFactory"></param>
        public InvertibleHybridBloomFilter(
            IBloomFilterConfiguration<TEntity,TId, int, TCount> bloomFilterConfiguration) : base(bloomFilterConfiguration)
        {
            _reverseBloomFilter = new InvertibleReverseBloomFilter<KeyValuePair<TId, int>, TId, TCount>(
                bloomFilterConfiguration.ConvertToKeyValueHash());
        }
        #endregion

        #region Implementation of Bloom Filter public contract

        public override void Add(TEntity item)
        {
            base.Add(item);
            _reverseBloomFilter.Add(new KeyValuePair<TId, int>(Configuration.GetId(item), Configuration.EntityHash(item)));
        }

        public override void Remove(TEntity item)
        {
            base.Remove(item);
            _reverseBloomFilter.Remove(new KeyValuePair<TId, int>(Configuration.GetId(item), Configuration.EntityHash(item)));
        }

        public override void RemoveKey(TId key)
        {
            throw new NotSupportedException("RemoveKey is not supported for an invertible hybrid Bloom filter. Please use a regular IBF.");
        }

        public override void Initialize(long capacity, long m, uint k)
        {
            base.Initialize(capacity, m, k);
            _reverseBloomFilter.Initialize(capacity, m, k);
            Data.ReverseFilter = _reverseBloomFilter.Extract();
        }

        /// <summary>
        /// Restore the data of the Bloom filter
        /// </summary>
        /// <param name="data"></param>
        public override void Rehydrate(IInvertibleBloomFilterData<TId, int, TCount> data)
        {
            if (data?.ReverseFilter == null)
                throw new ArgumentException("Data and value filter data are required for a hybrid estimator.", nameof(data));
            base.Rehydrate(data);
            _reverseBloomFilter.Rehydrate(data.ReverseFilter);
        }
        #endregion
    }
}