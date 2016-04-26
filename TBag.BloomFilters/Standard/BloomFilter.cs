namespace TBag.BloomFilters.Standard
{
    using System;
    using System.Collections;
    using Configurations;
    using System.Linq;
    using Invertible.Configurations;
    using System.Diagnostics.Contracts;    /// <summary>
                                           /// A simple Bloom filter
                                           /// </summary>
                                           /// <remarks>Not public for now</remarks>
    public class BloomFilter<TKey> : 
        IBloomFilter<TKey> where TKey : struct
    {
        private readonly IBloomFilterConfiguration<TKey, int> _configuration;
        private FastBitArray _data;
        private uint _hashFunctionCount;
        private long _capacity;
        private long _blockSize;
        private static readonly byte[] EmptyByteArray = new byte[0];

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
            _data = new FastBitArray((int) m);
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
            foreach (int position in _configuration
                .Probe(BlockSize, _hashFunctionCount, _configuration.IdHash(value)))
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
            foreach (int position in _configuration
                .Probe(BlockSize, _hashFunctionCount, _configuration.IdHash(value)))
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
                .Probe(BlockSize, _hashFunctionCount, _configuration.IdHash(value))
                .All(position => _data.Get((int)position));
        }

        /// <summary>
        /// Extract the Bloom filter data
        /// </summary>
        /// <returns></returns>
        public virtual BloomFilterData Extract()
        {
            return new BloomFilterData
            {
                Bits = _data?.ToBytes(),
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
        public virtual void Rehydrate(IBloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            _hashFunctionCount = bloomFilterData.HashFunctionCount;
            _capacity = bloomFilterData.Capacity;
            _blockSize = bloomFilterData.BlockSize;
            ItemCount = bloomFilterData.ItemCount;
            _data = new FastBitArray(bloomFilterData.Bits?? EmptyByteArray)
            {
                Length = (int)_blockSize
            };
        }

        /// <summary>
        /// Intersect with a Bloom filter. 
        /// </summary>
        /// <param name="bloomFilterData"></param>
        /// <remarks>Results in only retaining the keys the filters have in common.</remarks>
        public virtual void Intersect(IBloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            var foldedSize = _configuration.FoldingStrategy?.GetFoldFactors(_blockSize, bloomFilterData.BlockSize);
            if (bloomFilterData.BlockSize != BlockSize &
                foldedSize == null)
            {
                throw new ArgumentException("Bloom filters of different sizes cannot be intersected.", nameof(bloomFilterData));
            }
            var otherBitArray = new FastBitArray(bloomFilterData.Bits ?? EmptyByteArray)
            {
                Length = (int)bloomFilterData.BlockSize
            };
            if (foldedSize != null &&
                (foldedSize.Item2 != 1 ||
                foldedSize.Item1 != 1))
            {
                if (foldedSize.Item1 != 1)
                {
                    Fold((uint)foldedSize.Item1, true);
                }
                if (foldedSize.Item2 != 1)
                {
                    for (var i = 0; i < _data.Length; i++)
                    {
                        _data.Set(i, _data.Get(i) & GetFolded(otherBitArray, i, (uint)foldedSize.Item2));
                    }
                    ItemCount = EstimateItemCount(_data, _hashFunctionCount);
                    return;
                }
            }
            _data = _data.And(otherBitArray);
            ItemCount = EstimateItemCount(_data, _hashFunctionCount);
        }

        /// <summary>
        /// Add a Bloom filter (union)
        /// </summary>
        /// <param name="bloomFilter"></param>
        /// <remarks>Results in all keys from the filters.</remarks>
        public virtual void Add(IBloomFilter<TKey> bloomFilter)
        {
            if (bloomFilter == null) return;
            Add(bloomFilter.Extract());
        }
        /// <summary>
        /// Add a Bloom filter (union)
        /// </summary>
        /// <param name="bloomFilterData"></param>
        /// <remarks>Results in all keys from the filters.</remarks>
        public virtual void Add(IBloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            var foldedSize = _configuration.FoldingStrategy?.GetFoldFactors(_blockSize, bloomFilterData.BlockSize);
            if (bloomFilterData.BlockSize != BlockSize &
                foldedSize == null)
            {
                throw new ArgumentException("Bloom filters of different sizes cannot be added.", nameof(bloomFilterData));
            }
            var otherBitArray = new FastBitArray(bloomFilterData.Bits ?? EmptyByteArray)
            {
                Length = (int)bloomFilterData.BlockSize
            };
            if (foldedSize != null &&
                (foldedSize.Item2 != 1 ||
                foldedSize.Item1 != 1))
            {
                if (foldedSize.Item1 != 1)
                {
                    Fold((uint)foldedSize.Item1, true);
                }
                if (foldedSize.Item2 != 1)
                {
                    for (var i = 0; i < _data.Length; i++)
                    {
                        _data.Set(i, _data.Get(i) | GetFolded(otherBitArray, i, (uint)foldedSize.Item2));
                    }
                    ItemCount = ItemCount + bloomFilterData.ItemCount;
                    return;
                }
            }
            _data = _data.Or(otherBitArray);
            ItemCount = ItemCount + bloomFilterData.ItemCount;
        }

        /// <summary>
        /// Subtract the given Bloom filter, resulting in the difference.
        /// </summary>
        /// <param name="bloomFilterData"></param>
        /// <remarks>Results in the symmetric difference of the two Bloom filters.</remarks>
        public virtual void Subtract(IBloomFilterData bloomFilterData)
        {
            if (bloomFilterData == null) return;
            var foldedSize = _configuration.FoldingStrategy?.GetFoldFactors(_blockSize, bloomFilterData.BlockSize);
            if (bloomFilterData.BlockSize != BlockSize &
                foldedSize == null)
            {
                throw new ArgumentException("Bloom filters of different sizes cannot be subtracted.", nameof(bloomFilterData));
            }
            var otherBitArray = new FastBitArray(bloomFilterData.Bits ?? EmptyByteArray)
            {
                Length = (int)bloomFilterData.BlockSize
            };
            if (foldedSize != null &&
                (foldedSize.Item2 != 1 ||
                foldedSize.Item1 != 1))
            {
                if (foldedSize.Item1 != 1)
                {
                    Fold((uint)foldedSize.Item1, true);
                }
                if (foldedSize.Item2 != 1)
                {
                    for (var i = 0; i < _data.Length; i++)
                    {
                        _data.Set(i, _data.Get(i) ^ GetFolded(otherBitArray, i, (uint)foldedSize.Item2));
                    }
                    ItemCount = EstimateItemCount(_data, _hashFunctionCount);
                    return;
                }
            }
            _data = _data.Xor(otherBitArray);
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
            var result = _data.Fold(factor, inPlace);
            if (inPlace)
            {
                _capacity = _capacity / factor;
                _blockSize = result.Length;
                return this;
            }
            var bloomFilter = new BloomFilter<TKey>(_configuration);
            bloomFilter.Rehydrate(new BloomFilterData
            {
                Bits = result.ToBytes(),
                ItemCount = ItemCount,
                Capacity = _capacity/factor,
                BlockSize = result.Length,
                HashFunctionCount = _hashFunctionCount
            });
            return bloomFilter;
        }

        public BloomFilter<TKey> Compress(bool inPlace = false)
        {
            var foldFactor = _configuration?
                .FoldingStrategy?
                .FindCompressionFactor(_configuration, _blockSize, _capacity, ItemCount);
            if (foldFactor == null) return null;
           
            return Fold(foldFactor.Value, inPlace);
        }

        private static bool GetFolded(FastBitArray bitArray, int position, uint foldFactor)
        {
            if (foldFactor == 1) return bitArray[position];
            var foldedSize = bitArray.Length/foldFactor;
            for (var i = 0; i < foldFactor; i++)
            {
                if (bitArray.Get(position + i * (int)foldedSize))
                    return true;
            }
            return false;
        }

        private static long EstimateItemCount(FastBitArray array, uint hashFunctionCount)
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
