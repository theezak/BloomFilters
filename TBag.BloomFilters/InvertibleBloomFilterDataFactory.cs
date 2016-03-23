namespace TBag.BloomFilters
{
    using System;
  
    /// <summary>
    /// Implementation of <see cref="IInvertibleBloomFilterDataFactory"/>.
    /// </summary>
    public class InvertibleBloomFilterDataFactory : IInvertibleBloomFilterDataFactory
    {
        /// <summary>
        /// Create new Bloom filter data based upon the size and the hash function count.
        /// </summary>
        /// <typeparam name="TId">Type of the identifier</typeparam>
        /// <typeparam name="THash">Type of the hash</typeparam>
        /// <typeparam name="TCount">Type of the counter</typeparam>
        /// <param name="m">Size per hash function</param>
        /// <param name="k">The number of hash functions.</param>
        /// <returns>The Bloom filter data</returns>
        public InvertibleBloomFilterData<TId, THash, TCount> Create<TId, THash, TCount>(long m, uint k)
            where TId : struct
            where TCount : struct
            where THash : struct
        {
            if (m < 1) // from overflow in bestM calculation
                throw new ArgumentOutOfRangeException(
                    nameof(m),
                    "The provided capacity and errorRate values would result in an array of length > long.MaxValue. Please reduce either the capacity or the error rate.");
            return new InvertibleBloomFilterData<TId, THash, TCount>
            {
                HashFunctionCount = k,
                BlockSize = m,
                Counts = new TCount[m * k],
                IdSums = new TId[m * k],
                HashSums = new THash[m * k]
            };
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
