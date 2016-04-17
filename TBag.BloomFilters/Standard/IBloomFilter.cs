namespace TBag.BloomFilters.Standard
{
    internal interface IBloomFilter<TKey> where TKey : struct
    {
        /// <summary>
        /// The item count.
        /// </summary>
        /// <remarks>Provide an estimate.</remarks>
        long ItemCount { get; }

        /// <summary>
        /// The capacity.
        /// </summary>
        long Capacity { get; }

        /// <summary>
        /// The block size.
        /// </summary>
        long BlockSize { get; }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <param name="errorRate">The desired error rate (between 0 and 1).</param>
        /// <param name="foldFactor"></param>
        void Initialize(long capacity, float errorRate, int foldFactor = 0);

        /// <summary>
        /// Initialize the Bloom filter
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <param name="m">Size per hash function</param>
        /// <param name="k">Number of hash functions.</param>
        void Initialize(long capacity, long m, uint k);

        /// <summary>
        /// Add a value to the Bloom filter.
        /// </summary>
        /// <param name="value"></param>
        void Add(TKey value);

        /// <summary>
        /// Remove a value from the Bloom filter
        /// </summary>
        /// <param name="value"></param>
        /// <remarks>Not the best thing to do. Use a counting Bloom filter instead when you need removal. Throw a not supported exception instead?</remarks>
        void Remove(TKey value);

        /// <summary>
        /// Determine if a value is in the Bloom filter.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Contains(TKey value);

        /// <summary>
        /// Extract the Bloom filter data
        /// </summary>
        /// <returns></returns>
        BloomFilterData Extract();

        /// <summary>
        /// Load the Bloom filter data into the Bloom filter
        /// </summary>
        /// <param name="bloomFilterData"></param>
        void Rehydrate(BloomFilterData bloomFilterData);

        /// <summary>
        /// Intersect with a Bloom filter. 
        /// </summary>
        /// <param name="bloomFilterData"></param>
        /// <remarks>Results in only retaining the keys the filters have in common.</remarks>
        void Intersect(BloomFilterData bloomFilterData);

        /// <summary>
        /// Add a Bloom filter (union)
        /// </summary>
        /// <param name="bloomFilterData"></param>
        /// <remarks>Results in all keys from the filters.</remarks>
        void Add(BloomFilterData bloomFilterData);

        /// <summary>
        /// Subtract the given Bloom filter, resulting in the difference.
        /// </summary>
        /// <param name="bloomFilterData"></param>
        /// <remarks>Results in only retaining the keys the filters do not have in common.</remarks>
        void Subtract(BloomFilterData bloomFilterData);

        /// <summary>
        /// Fold the Bloom filter by the given factor.
        /// </summary>
        /// <param name="factor"></param>
        /// <param name="inPlace"></param>
        /// <returns></returns>
        BloomFilter<TKey> Fold(uint factor, bool inPlace = false);

        /// <summary>
        /// Compress the Bloom filter.
        /// </summary>
        /// <param name="inPlace"></param>
        /// <returns></returns>
        BloomFilter<TKey> Compress(bool inPlace = false);
    }
}
