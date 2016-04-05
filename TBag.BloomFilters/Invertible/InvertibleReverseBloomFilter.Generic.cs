namespace TBag.BloomFilters.Invertible
{
    using Configurations;
    using System;
  
    /// <summary>
    /// An invertible Bloom filter that stores key-value pairs.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TId">The entity identifier type</typeparam>
    /// <typeparam name="TCount">The occurence count</typeparam>
    public class InvertibleReverseBloomFilter<TEntity, TId,TCount> : 
        InvertibleBloomFilter<TEntity, TId,TCount>
        where TCount : struct
        where TId : struct
    {
       #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bloomFilterConfiguration">The Bloom filter configuration</param>
        public InvertibleReverseBloomFilter(
            IInvertibleBloomFilterConfiguration<TEntity, TId, int, TCount> bloomFilterConfiguration) :
                base(bloomFilterConfiguration)
        {
            ValidateConfiguration = false;
        }
        #endregion

        #region Implementation of Bloom Filter public contract
        /// <summary>
        /// Initialize the Bloom filter
        /// </summary>
        /// <param name="capacity">The capacity (number of elements to store in the Bloom filter)</param>
        /// <param name="m">The size of the Bloom filter per hash function</param>
        /// <param name="k">The number of the hash function</param>
        public override void Initialize(long capacity, long m, uint k)
        {
            base.Initialize(capacity, m, k);       
            Data.IsReverse = true;
        }

        /// <summary>
        /// Restore the data of the Bloom filter
        /// </summary>
        /// <param name="data">The data to restore</param>
        public override void Rehydrate(IInvertibleBloomFilterData<TId, int, TCount> data)
        {
            if (data == null) return;
              if (!data.IsReverse)
            {
                throw new ArgumentException("Reverse IBF can only rehydrate reverse IBF data.", nameof(data));
            }
            base.Rehydrate(data);
        }

        /// <summary>
        /// Determine if the Bloom filter contains the key
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Not supported</exception>
        public override bool ContainsKey(TId key)
        {
            throw new NotSupportedException("ContainsKey not supported on a reverse IBF");
        }

        /// <summary>
        /// Remove the given key
        /// </summary>
        /// <param name="key">Key to remove</param>
        /// <exception cref="NotSupportedException">Not supported</exception>
        public override void RemoveKey(TId key)
        {
            throw new NotSupportedException("RemoveKey not supported on a reverse IBF");
        }

        #endregion

        #region Methods
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override IInvertibleBloomFilter<TEntity, TId, TCount> CreateNewInstance(IInvertibleBloomFilterData<TId, int, TCount> data)
        {
            var instance = new InvertibleReverseBloomFilter<TEntity,TId,TCount>(Configuration);
            instance.Rehydrate(data);
            return instance;
        }

        /// <summary>
        /// Validate the data.
        /// </summary>
        protected override bool ValidateData()
        {
            if (Data == null)
            {
                throw new InvalidOperationException("The invertible Bloom filter was not initialized or rehydrated.");
            }
            return true;
        }

        #endregion
    }
}