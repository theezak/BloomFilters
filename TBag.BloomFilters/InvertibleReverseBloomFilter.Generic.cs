namespace TBag.BloomFilters
{
    using System;

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
       #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bloomFilterConfiguration"></param>
        /// <param name="invertibleBloomFilterDataFactory"></param>
        public InvertibleReverseBloomFilter(
            IBloomFilterConfiguration<TEntity, TId, int, TCount> bloomFilterConfiguration) : 
            base(bloomFilterConfiguration)
        { }
        #endregion

        #region Implementation of Bloom Filter public contract

        public override void Initialize(long capacity, long m, uint k)
        {
            base.Initialize(capacity, m, k);
            Data.IsReverse = true;
        }

        public override bool ContainsKey(TId key)
        {
            throw new NotSupportedException("ContainsKey not supported on a reverse IBF");
        }

        public override void RemoveKey(TId key)
        {
            throw new NotSupportedException("RemoveKey not supported on a reverse IBF");
        }
        #endregion
    }
}