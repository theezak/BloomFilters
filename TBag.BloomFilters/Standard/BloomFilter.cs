namespace TBag.BloomFilters.Standard
{
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using TBag.BloomFilters.Configurations;

    /// <summary>
    /// A simple Bloom filter
    /// </summary>
    public class BloomFilter<TKey> : IBloomFilter<TKey,int>
        where TKey : struct
    {
        private readonly IBloomFilterConfiguration<TKey, int> _configuration;
        private BitArray _data;
        private uint _hashFunctionCount;
        private long _capacity;
        private long _blockSize;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        public BloomFilter(IBloomFilterConfiguration<TKey, int> configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="capacity">The capacity</param>
        /// <param name="foldFactor"></param>
        public void Initialize(long capacity, int foldFactor = 0)
        {
            Initialize(capacity, _configuration.BestErrorRate(capacity), foldFactor);
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <param name="errorRate">The desired error rate (between 0 and 1).</param>
        /// <param name="foldFactor"></param>
        public void Initialize(long capacity, float errorRate, int foldFactor = 0)
        {
            Initialize(
                capacity,
                _configuration.BestCompressedSize(capacity, errorRate, foldFactor),
                _configuration.BestHashFunctionCount(capacity, errorRate));
        }

        /// <summary>
        /// Initialize the Bloom filter
        /// </summary>
        /// <param name="capacity">Capacity</param>
        /// <param name="m">Size per hash function</param>
        /// <param name="k">Number of hash functions.</param>
        public virtual void Initialize(long capacity, long m, uint k)
        {
            // validate the params are in range
            if (!_configuration.Supports(capacity, m))
            {
                throw new ArgumentOutOfRangeException(
                    $"The size {m} of the Bloom filter is not large enough to hold {capacity} items.");
            }
            _data = new BitArray((int)(m * k));
            _hashFunctionCount = k;
            _capacity = capacity;
            _blockSize = m;
        }

        public virtual void Add(TKey value)
        {
            foreach(var position in _configuration.Hashes(_configuration.IdHash(value), _hashFunctionCount))
            {
                _data.Set((int)position, true);
            }
        }

        /// <summary>
        /// Remove a key
        /// </summary>
        /// <param name="value"></param>
        /// <remarks>Not the best thing to do. Use a counting Bloom filter instead when you need removal. Throw a not supported exception instead?</remarks>
        public virtual void Remove(TKey value)
        {
            foreach (var position in _configuration.Hashes(_configuration.IdHash(value), _hashFunctionCount))
            {
                _data.Set((int)position, false);
            }
        }

        public virtual BloomFilterData Extract()
        {
            return new BloomFilterData
            {
                Bits = _data.ToBytes(),
                Capacity = _capacity,
                BlockSize = _blockSize,
                HashFunctionCount = _hashFunctionCount
            };
        }

        public virtual void Rehydrate(BloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            _hashFunctionCount = bloomFilterData.HashFunctionCount;
            _capacity = bloomFilterData.Capacity;
            _blockSize = bloomFilterData.BlockSize;
            _data = new BitArray(bloomFilterData.Bits)
            {
                Length = (int)(_blockSize * _hashFunctionCount)
            };
        }

        public virtual void Intersect(BloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            //todo: check compatibility
            _data.And(new BitArray(bloomFilterData.Bits) { Length = (int)(bloomFilterData.BlockSize * bloomFilterData.HashFunctionCount) });
        }

        public virtual void Add(BloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            //todo: check compatibility
            _data.Or(new BitArray(bloomFilterData.Bits) { Length = (int)(bloomFilterData.BlockSize * bloomFilterData.HashFunctionCount) });

        }

        public virtual void Fold()
        {
            //TODO
        }

        public void Compress()
        {
            //TODO
        }
    }
}
