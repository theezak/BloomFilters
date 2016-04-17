namespace TBag.BloomFilters.Standard
{
    using System;
    using System.Collections;
    using Configurations;
    using System.Linq;

    /// <summary>
    /// A simple Bloom filter
    /// </summary>
    /// <remarks>Not public for now</remarks>
    internal class BloomFilter<TKey> : 
        IBloomFilter<TKey> where TKey : struct
    {
        private readonly IBloomFilterConfiguration<TKey, int> _configuration;
        private BitArray _data;
        private uint _hashFunctionCount;
        private long _capacity;
        private long _blockSize;

        #region Properties

        /// <summary>
        /// When <c>true</c> the configuration will be validated, else <c>false</c>.
        /// </summary>
        protected bool ValidateConfiguration { get; set; }

        /// <summary>
        /// The item count.
        /// </summary>
        /// <remarks>Provide an estimate.</remarks>
        public virtual long ItemCount { get; private set; }

        /// <summary>
        /// The capacity.
        /// </summary>
        public virtual long Capacity => _capacity;

        /// <summary>
        /// The block size.
        /// </summary>
        public virtual long BlockSize => _blockSize;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        public BloomFilter(IBloomFilterConfiguration<TKey, int> configuration)
        {
            _configuration = configuration;
        }

        #endregion

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
            _data = new BitArray((int) (m*k));
            _hashFunctionCount = k;
            _capacity = capacity;
            _blockSize = m;
        }

        /// <summary>
        /// Add a value to the Bloom filter.
        /// </summary>
        /// <param name="value"></param>
        public virtual void Add(TKey value)
        {
            foreach (var position in _configuration.Hashes(_configuration.IdHash(value), _hashFunctionCount))
            {
                _data.Set(position, true);
            }
            ItemCount++;
        }

        /// <summary>
        /// Remove a value from the Bloom filter
        /// </summary>
        /// <param name="value"></param>
        /// <remarks>Not the best thing to do. Use a counting Bloom filter instead when you need removal. Throw a not supported exception instead?</remarks>
        public virtual void Remove(TKey value)
        {
            foreach (var position in _configuration.Hashes(_configuration.IdHash(value), _hashFunctionCount))
            {
                _data.Set(position, false);
            }
            ItemCount--;
        }

        /// <summary>
        /// Determine if a value is in the Bloom filter.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual bool Contains(TKey value)
        {
            return _configuration
                .Hashes(_configuration.IdHash(value), _hashFunctionCount)
                .All(position => _data.Get(position));
        }

        /// <summary>
        /// Extract the Bloom filter data
        /// </summary>
        /// <returns></returns>
        public virtual BloomFilterData Extract()
        {
            return new BloomFilterData
            {
                Bits = _data.ToBytes(),
                Capacity = _capacity,
                BlockSize = _blockSize,
                ItemCount = ItemCount,
                HashFunctionCount = _hashFunctionCount
            };
        }

        /// <summary>
        /// Load the Bloom filter data into the Bloom filter
        /// </summary>
        /// <param name="bloomFilterData"></param>
        public virtual void Rehydrate(BloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            _hashFunctionCount = bloomFilterData.HashFunctionCount;
            _capacity = bloomFilterData.Capacity;
            _blockSize = bloomFilterData.BlockSize;
            ItemCount = bloomFilterData.ItemCount;
            _data = new BitArray(bloomFilterData.Bits)
            {
                Length = (int) (_blockSize*_hashFunctionCount)
            };
        }

        /// <summary>
        /// Intersect with a Bloom filter. 
        /// </summary>
        /// <param name="bloomFilterData"></param>
        /// <remarks>Results in only retaining the keys the filters have in common.</remarks>
        public virtual void Intersect(BloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            //todo: check compatibility
            _data.And(new BitArray(bloomFilterData.Bits)
            {
                Length = (int) (bloomFilterData.BlockSize*bloomFilterData.HashFunctionCount)
            });
            ItemCount = EstimateItemCount(_data, _hashFunctionCount);
        }

        /// <summary>
        /// Add a Bloom filter (union)
        /// </summary>
        /// <param name="bloomFilterData"></param>
        /// <remarks>Results in all keys from the filters.</remarks>
        public virtual void Add(BloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            //todo: check compatibility
            _data.Or(new BitArray(bloomFilterData.Bits)
            {
                Length = (int) (bloomFilterData.BlockSize*bloomFilterData.HashFunctionCount)
            });
            ItemCount = ItemCount + bloomFilterData.ItemCount;
        }

        /// <summary>
        /// Subtract the given Bloom filter, resulting in the difference.
        /// </summary>
        /// <param name="bloomFilterData"></param>
        /// <remarks>Results in the symmetric difference of the two Bloom filters.</remarks>
        public virtual void Subtract(BloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            //todo: check compatibility
            _data.Xor(new BitArray(bloomFilterData.Bits)
            {
                Length = (int) (bloomFilterData.BlockSize*bloomFilterData.HashFunctionCount)
            });
            ItemCount = EstimateItemCount(_data, _hashFunctionCount);
        }

        /// <summary>
        /// Fold the Bloom filter by the given factor.
        /// </summary>
        /// <param name="factor"></param>
        /// <param name="inPlace"></param>
        /// <returns></returns>
        public virtual BloomFilter<TKey> Fold(uint factor, bool inPlace = false)
        {
            if (factor <= 0)
                throw new ArgumentException($"Fold factor should be a positive number (given value was {factor}.");
            if (_blockSize%factor != 0)
            {
                throw new ArgumentException(
                    $"Bloom filter of size {_blockSize} cannot be folded by a factor {factor}.", nameof(factor));
            }
            var newBlockSize = _blockSize/factor;       
            var newCapacity = _capacity/factor;
            var result = inPlace ? _data : new BitArray((int) newBlockSize);
            for (var i = 0; i < (int) newBlockSize; i++)
            {
                result.Set(i, GetFolded(_data, i, (int) factor));
            }
            if (inPlace)
            {
                result.Length = (int) newBlockSize;
                _capacity = newCapacity;
                _blockSize = newBlockSize;
                return this;
            }
            var bloomFilter = new BloomFilter<TKey>(_configuration);
            bloomFilter.Rehydrate(new BloomFilterData
            {
                Bits = result.ToBytes(),
                ItemCount = ItemCount,
                Capacity = newCapacity,
                BlockSize = newBlockSize,
                HashFunctionCount = _hashFunctionCount
            });
            return bloomFilter;
        }

        public BloomFilter<TKey> Compress(bool inPlace = false)
        {
            var foldFactor = _configuration?.FoldingStrategy?.FindCompressionFactor(_blockSize, _capacity, ItemCount);
            if (foldFactor == null) return null;
            return Fold(foldFactor.Value, inPlace);
        }

        private static bool GetFolded(BitArray bitArray, int position, int foldFactor)
        {
            if (foldFactor == 1) return bitArray[position];
            var foldedSize = bitArray.Length/foldFactor;
            for (var i = 0; i < foldFactor; i++)
            {
                if (!bitArray.Get(position + i*foldedSize))
                    return false;
            }
            return true;
        }

        private static long EstimateItemCount(BitArray array, uint hashFunctionCount)
        {
            var bitCount = 0L;
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i])
                {
                    bitCount++;
                }
            }
            return bitCount / hashFunctionCount;
        }
    }
}
